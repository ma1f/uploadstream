using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using UploadStream;

namespace File.Api.Controllers {

    [Route("api")]
    [Produces("application/json")]
    public class UploadController : Controller {

        const int BUF_SIZE = 4096;

        public class Base64model {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Base64 { get; set; }
        }

        public class UploadModel {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<IFormFile> Files { get; set; }
        }

        class StreamModel {
            public string Name { get; set; }
            public string Description { get; set; }
        }

        [HttpPost("base64")]
        public IActionResult Base64([FromBody] Base64model model) {
            var bytes = Convert.FromBase64String(model.Base64);

            return Ok(new { model.Name, model.Description, Count = bytes.Length });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(UploadModel model) {
            byte[] buffer = new byte[BUF_SIZE];

            foreach (var s in model.Files)
                using (var stream = s.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;

            return Ok(new { model.Name, model.Description, model.Files.Count });
        }

        [HttpPost("stream")]
        [DisableFormModelBinding]
        public async Task<IActionResult> ControllerModelStream() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<StreamModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Model = model, Files = files });
        }

        [HttpPost("stream/modeless")]
        [DisableFormModelBinding]
        public async Task<IActionResult> ControllerStream() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            await this.StreamFiles(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Files = files });
        }

        [HttpPost("stream/binding")]
        public async Task<IActionResult> ControllerStreamBindingEnabled() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<StreamModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Model = model, Files = files });
        }

        [HttpPost("stream/binding/model")]
        public async Task<IActionResult> ControllerModelStreamBindingEnabled(UploadModel model) {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var streamModel = await this.StreamFiles<StreamModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { model, streamModel, Files = files });
        }

        [HttpPost("model/bindingdisabled")]
        [DisableFormModelBinding]
        public async Task<IActionResult> ModelBindingDisabled(UploadModel model) {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var streamModel = await this.StreamFiles<UploadModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { model, streamModel, files.Count });
        }
    }
}
