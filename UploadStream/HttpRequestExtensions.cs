﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace UploadStream {
    public static class HttpRequestExtensions {
        static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<FormValueProvider> StreamFilesModel(this HttpRequest request, Action<IFormFile> func) {
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
                throw new Exception($"Expected a multipart request, but got {request.ContentType}");

            // Used to accumulate all the form url encoded key value pairs in the request.
            var formAccumulator = new KeyValueAccumulator();
            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(request.ContentType), _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null) {
                var hasHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDispositionHeader);
                
                if (hasHeader && contentDispositionHeader.IsFileDisposition()) {
                    FileMultipartSection fileSection = section.AsFileSection();
                    Debug.WriteLine($"{fileSection.FileStream.Length}, {fileSection.Name}, {fileSection.FileName}");

                    // process file stream
                    func(new MultipartFile(fileSection.FileStream, fileSection.Name, fileSection.FileName) {
                        ContentType = fileSection.Section.ContentType,
                        ContentDisposition = fileSection.Section.ContentDisposition
                    });
                } else if (hasHeader && contentDispositionHeader.IsFormDisposition()) {
                    // Content-Disposition: form-data; name="key"
                    // Do not limit the key name length here because the multipart headers length limit is already in effect.
                    var key = HeaderUtilities.RemoveQuotes(contentDispositionHeader.Name);
                    var encoding = section.GetEncoding();
                    using (var streamReader = new StreamReader(section.Body, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true)) {
                        // The value length limit is enforced by MultipartBodyLengthLimit
                        var value = await streamReader.ReadToEndAsync();
                        if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            value = String.Empty;

                        formAccumulator.Append(key.Value, value);

                        if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                    }
                }

                // Drains any remaining section body that has not been consumed and reads the headers for the next section.
                section = request.Body.Position == request.Body.Length ? null : await reader.ReadNextSectionAsync();
            }
            // Bind form data to a model
            var formValueProvider = new FormValueProvider(BindingSource.Form, new FormCollection(formAccumulator.GetResults()), CultureInfo.CurrentCulture);

            return formValueProvider;
        }
    }
}