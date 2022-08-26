using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Html;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using NUglify;

namespace BililiveRecorder.Web
{
    [Controller]
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class DynamicHtmlController : Controller
    {
        private static string? cachedIndexHtml;
        private readonly CompositeFileProvider fileProvider;

        public DynamicHtmlController(CompositeFileProvider fileProvider)
        {
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        [Route("/", Name = "Home Page"), HttpGet]
        public ActionResult GetHomePage()
        {
            if (cachedIndexHtml is null)
            {
                using var file = this.fileProvider.GetFileInfo("/index.html").CreateReadStream();
                using var reader = new StreamReader(file, Encoding.UTF8);
                var html = reader.ReadToEnd();
                cachedIndexHtml = html
                    .Replace("__VERSION__", GitVersionInformation.FullSemVer)
                    .Replace("__FULL_VERSION__", GitVersionInformation.InformationalVersion)
                    ;
            }

            return this.Content(cachedIndexHtml, "text/html", Encoding.UTF8);
        }

        [Route("/ui/", Name = "WebUI Html"), HttpGet]
        public async Task GetWebUIAsync()
        {
            var parser = new HtmlParser();
            var fileInfo = this.fileProvider.GetFileInfo("/ui/index.html");

            using var injectionScriptStream = new StreamReader(this.fileProvider.GetFileInfo(".webui_injection.js").CreateReadStream());
            var scriptContent = await injectionScriptStream.ReadToEndAsync();

            using var stream = fileInfo.CreateReadStream();
            using var document = await parser.ParseDocumentAsync(stream).ConfigureAwait(false);

            var spaPath = this.HttpContext.Items.ContainsKey("webui-spa-path") ? ((PathString)this.HttpContext.Items["webui-spa-path"]!) : this.Request.Path;

            spaPath.StartsWithSegments("/ui", out var remaining);

            var head = document.Head!;
            var template = document.CreateElement("template");
            template.Id = "delayed-init";
            head.AppendChild(template);

            var scripts = document.QuerySelectorAll("script[type='module']");
            var css = document.QuerySelectorAll("link[rel='stylesheet']");

            foreach (var script in scripts)
            {
                script.Remove();
                template.AppendChild(script);
            }

            foreach (var node in css)
            {
                node.Remove();
                template.AppendChild(node);
            }

            var initScript = document.CreateElement("script");
            initScript.TextContent = Uglify.Js(scriptContent).Code;
            initScript.SetAttribute("data-href", remaining);

            head.AppendChild(initScript);

            this.Response.StatusCode = 200;
            this.Response.ContentType = "text/html; encoding=utf-8";

            using var writer = new StreamWriter(this.Response.Body);
            document.ToHtml(writer, new MinifyMarkupFormatter());
        }
    }
}
