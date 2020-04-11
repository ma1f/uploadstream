using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace UploadStream {
    public class MultipartFile : IFormFile {

        // Stream.CopyTo method uses 80KB as the default buffer size.
        const int DefaultBufferSize = 80 * 1024;
        readonly Stream _stream;

        public string ContentType {
            get { return Headers["Content-Type"]; }
            set { Headers["Content-Type"] = value; }
        }

        public string ContentDisposition {
            get { return Headers["Content-Disposition"]; }
            set { Headers["Content-Disposition"] = value; }
        }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public long Length => _stream.Length;
        public string Name { get; set; }
        public string FileName { get; set; }

        public MultipartFile(Stream stream, string name, string filename) {
            _stream = stream;
            Name = name;
            FileName = filename;
        }

        public void CopyTo(Stream target) {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            _stream.CopyTo(target);
        }

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return _stream.CopyToAsync(target, DefaultBufferSize, cancellationToken);
        }

        public Stream OpenReadStream() {
            return _stream;
        }
    }
}
