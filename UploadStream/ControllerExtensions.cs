using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace UploadStream {
    public static class ControllerExtensions {
        /// <summary>
        /// Processes Multi-part HttpRequest streams via the specified delegate
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns>Returns multi-part form fields as the required generic model specified.</returns>
        public static async Task<T> StreamFiles<T>(this ControllerBase controller, Func<IFormFile, Task> func) where T : class, new() {
            var form = await controller.Request.StreamFilesModel(func);
            return await UpdateModel<T>(form, controller);
        }

        /// <summary>
        /// Processes Multi-part HttpRequest streams via the specified delegate, no model required for return
        /// </summary>
        /// <param name="func"></param>
        public static async Task StreamFiles(this Controller controller, Func<IFormFile, Task> func) {
            await controller.Request.StreamFilesModel(func);
        }

        static async Task<T> UpdateModel<T>(FormValueProvider form, ControllerBase controller) where T : class, new() {
            var model = new T();
            await controller.TryUpdateModelAsync<T>(model, prefix: "", valueProvider : form);
            controller.TryValidateModel(model);
            return model;
        }
    }
}
