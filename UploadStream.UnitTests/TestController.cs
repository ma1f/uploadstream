using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UploadStream.UnitTests {

    [Route("api")]
    [Produces("application/json")]
    public class TestController : Controller {

        const int BUF_SIZE = 4096;

        class TestModel {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public class TestFileModel {
            public int Id { get; set; }
            public string Name { get; set; }
            public IFormFile[] Files { get; set; }
        }
        public class TestRequiredModel {
            [Required]
            public int Id { get; set; }
            [Required, MinLength(5)]
            public string Name { get; set; }
            [Required]
            public string Description { get; set; }
        }

        [HttpPost("default")]
        public IActionResult Default(TestFileModel model) {
            return Ok(new { Model = model, ModelState.IsValid });
        }

        [HttpPost("default/nobinding")]
        [DisableFormModelBindingAttribute]
        public IActionResult DefaultNoModelBinding(TestFileModel model) {
            return Ok(new { Model = model, ModelState.IsValid });
        }

        [HttpPost("null")]
        [DisableFormModelBindingAttribute]
        public async Task<IActionResult> Null() {
            await this.StreamFiles(x => {
                throw new Exception("should not execute");
            });

            return Ok(new { ModelState.IsValid });
        }

        [HttpPost("nomodel")]
        [DisableFormModelBindingAttribute]
        public async Task<IActionResult> NoModel() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            await this.StreamFiles(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            return Ok(new { Files = files, ModelState.IsValid });
        }

        [HttpPost("model")]
        [DisableFormModelBindingAttribute]
        public async Task<IActionResult> Model() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<TestModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(new { Model = model, Files = files, ModelState });
        }

        [HttpPost("model/validation")]
        [DisableFormModelBindingAttribute]
        public async Task<IActionResult> ModelValidation() {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<TestRequiredModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(new { Model = model, Files = files, ModelState });
        }

        [HttpPost("model/bindingenabled")]
        public async Task<IActionResult> ModelBindingEnabled(TestFileModel bindingmodel) {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<TestModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(new { Model = model, BindingModel = bindingmodel, Files = files, ModelState.IsValid });
        }

        [HttpPost("model/bindingdisabled")]
        [DisableFormModelBindingAttribute]
        public async Task<IActionResult> ModelBindingDisabled(TestFileModel bindingmodel) {
            byte[] buffer = new byte[BUF_SIZE];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<TestModel>(async x => {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new { Model = model, BindingModel = bindingmodel, Files = files, ModelState.IsValid });
        }
    }
}
