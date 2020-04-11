using System.Text;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace UploadStream {
    public static class MultipartSectionExtensions {
        public static Encoding GetEncoding(this MultipartSection section) {
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
                return Encoding.UTF8;

            return mediaType.Encoding;
        }
    }
}
