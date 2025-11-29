using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fluid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using NUglify;

namespace BililiveRecorder.Web
{
    public class BililiveRecorderDirectoryFormatter : IDirectoryFormatter
    {
        private static readonly IFluidTemplate template;

        static BililiveRecorderDirectoryFormatter()
        {
            var fileProvider = new ManifestEmbeddedFileProvider(typeof(BililiveRecorderDirectoryFormatter).Assembly);
            using var file = fileProvider.GetFileInfo(".file_template.html").CreateReadStream();
            using var reader = new StreamReader(file);

            template = new FluidParser().Parse(reader.ReadToEnd());
        }

        public BililiveRecorderDirectoryFormatter()
        {
        }

        public Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents)
        {
            var options = new TemplateOptions();
            options.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            options.MemberAccessStrategy.Register<IFileInfo>();

            options.Filters.AddFilter("brec_url_encode", static (input, arguments, context) =>
            {
                return new Fluid.Values.StringValue(Uri.EscapeDataString(input.ToStringValue()));
            });

            var tc = new TemplateContext(options);
            tc.SetValue("path", (context.Request.PathBase + context.Request.Path).Value);
            tc.SetValue("files", contents.OrderBy(x => x.Name));

            var result = template.Render(tc);

            result = Uglify.Html(result).Code;

            var bytes = Encoding.UTF8.GetBytes(result);
            context.Response.ContentLength = bytes.Length;
            return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
