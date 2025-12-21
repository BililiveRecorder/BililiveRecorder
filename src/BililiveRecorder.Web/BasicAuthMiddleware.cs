using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace BililiveRecorder.Web
{
    public class BasicAuthMiddleware
    {
        private readonly RequestDelegate next;
        private readonly CompositeFileProvider fileProvider;
        private const string BasicAndSpace = "Basic ";

        private static string? Html401Page;

        public BasicAuthMiddleware(RequestDelegate next, CompositeFileProvider fileProvider)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            this.fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.RequestServices.GetService<BasicAuthCredential>() is not { } credential)
            {
                // 没有启用身份验证
                return this.next(context);
            }

            string? headerValue = context.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(headerValue) ||
                !headerValue.StartsWith(BasicAndSpace, StringComparison.OrdinalIgnoreCase))
            {
                return this.ResponseWith401Async(context);
            }

            var requestCredential = headerValue[BasicAndSpace.Length..].Trim();

            if (string.IsNullOrEmpty(requestCredential))
            {
                return this.ResponseWith401Async(context);
            }

            if (credential.EncoededValue.Equals(requestCredential, StringComparison.Ordinal))
            {
                return this.next(context);
            }
            else
            {
                return this.ResponseWith401Async(context);
            }
        }

        private async Task ResponseWith401Async(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "text/html";
            context.Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{BasicAndSpace}realm=\"BililiveRecorder {GitVersionInformation.FullSemVer}\"");

            if (Html401Page is null)
            {
                using var file = this.fileProvider.GetFileInfo("/401.html").CreateReadStream();
                using var reader = new StreamReader(file);
                var str = await reader.ReadToEndAsync().ConfigureAwait(false);
                Html401Page = str.Replace("__VERSION__", GitVersionInformation.FullSemVer).Replace("__FULL_VERSION__", GitVersionInformation.InformationalVersion);
            }

            await context.Response.WriteAsync(Html401Page, System.Text.Encoding.UTF8).ConfigureAwait(false);
        }
    }
}
