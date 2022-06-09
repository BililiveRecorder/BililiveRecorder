using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace BililiveRecorder.Web
{
    [Controller, Route("/", Name = "Home Page")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class IndexController : Controller
    {
        private static string? result;
        private readonly ManifestEmbeddedFileProvider fileProvider;

        public IndexController(ManifestEmbeddedFileProvider fileProvider)
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        [HttpGet]
        public ActionResult Get()
        {
            if (result is null)
            {
                using var file = this.fileProvider.GetFileInfo("/index.html").CreateReadStream();
                using var reader = new StreamReader(file, Encoding.UTF8);
                var html = reader.ReadToEnd();
                result = html
                    .Replace("__VERSION__", GitVersionInformation.FullSemVer)
                    .Replace("__FULL_VERSION__", GitVersionInformation.InformationalVersion)
                    ;
            }

            return this.Content(result, "text/html", Encoding.UTF8);
        }
    }
}
