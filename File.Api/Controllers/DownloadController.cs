using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace File.Api.Controllers {
    [Route("api")]
    public class DownloadController : Controller {

        static string BaseDir = Path.Combine(AppContext.BaseDirectory, "resources");

        readonly static Dictionary<string, string> _files = new Dictionary<string,string>{
            { "xs.png", "image/png" },
            {"sm.jpg", "image/jpeg" },
            {"md.jpg", "image/jpeg" },
            {"lg.jpg", "image/jpeg" },
            {"xl.exe", "application/octet-stream" },
            {"xxl.exe", "application/octet-stream" }
        };

        static Dictionary<string, string> _paths;
        static Dictionary<string, string> Paths => _paths ?? (_paths = _files.ToDictionary(x => x.Key, x => Path.Combine(BaseDir, x.Key)));

        static Dictionary<string, byte[]> _bytes;
        static Dictionary<string, byte[]> Bytes => _bytes ?? (_bytes = Paths.ToDictionary(x => x.Key, x => System.IO.File.ReadAllBytes(x.Value)));
        
        [HttpGet("ping")]
        public IActionResult Ping() {
            return Ok();
        }

        [HttpGet("base64/{key}")]
        public async Task<string> Base64([FromRoute] string key) {
            return Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(Paths[key]));
        }

        [HttpGet("base64/{key}/memory")]
        public string Base64mem([FromRoute] string key) {
            return Convert.ToBase64String(Bytes[key]);
        }

        [HttpGet("file/{key}")]
        public IActionResult Download([FromRoute] string key) {
            return File($"~/resources/{key}", _files[key]);
        }

        [HttpGet("file/{key}/memory")]
        public IActionResult DownloadMem([FromRoute] string key) {
            return File(Bytes[key], _files[key]);
        }

        [HttpGet("file/{key}/filestream")]
        public IActionResult DownloadStream([FromRoute] string key) {
            var fileStream = new FileStream(Paths[key], FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64);
            return File(fileStream, _files[key]);
        }

        [HttpGet("stream/{key}")]
        public IActionResult Stream([FromRoute] string key) {
            var fileStream = new FileStream(Paths[key], FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 64);
            return new FileStreamResult(fileStream, _files[key]);
        }

        [HttpGet("stream/{key}/memory")]
        public IActionResult StreamMem([FromRoute] string key) {
            var memStream = new MemoryStream(Bytes[key]);
            return new FileStreamResult(memStream, _files[key]);
        }

    }
}
