using System;
using System.IO;

using Microsoft.Net.Http.Headers;

namespace UploadStream {
    public static class MultipartRequestHelper {
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit) {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary))
                throw new InvalidDataException("Missing content-type boundary.");

            if (boundary.Length > lengthLimit)
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");

            return boundary;
        }

        public static bool IsMultipartContentType(string contentType) {
            return !string.IsNullOrEmpty(contentType) && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

    }
}
