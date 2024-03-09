using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace BililiveRecorder.Web
{
    public class OpenAccessWarningMiddleware
    {
        private readonly RequestDelegate next;

        public OpenAccessWarningMiddleware(RequestDelegate next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (context.RequestServices.GetService<BasicAuthCredential>() is not null)
            {
                // 启用了身份验证，不需要警告
                return this.next(context);
            }

            if (sourceIpNotLan(context) || haveReverseProxyHeaders(context) || haveCustomHostValue(context))
            {
                context.Response.StatusCode = 412;
                var accept = context.Request.Headers[HeaderNames.Accept];
                if (accept.Contains("text/html"))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    return context.Response.WriteAsync("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Access Denied</title></head>" +
                    "<body><h1>Access Denied</h1><p>Open access from the internet detected. Please enable basic authentication " +
                    "or disable this warning by setting the environment variable \"BREC_HTTP_OPEN_ACCESS\".</p>" +
                    "<p>检测到非局域网无密码访问。请设置用户名密码或通过设置环境变量 \"BREC_HTTP_OPEN_ACCESS\" 禁用此警告。</p>" +
                    "<hr><p>录播姬 BililiveRecorder " + GitVersionInformation.FullSemVer + "</p></body></html>\n");
                }
                else
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    return context.Response.WriteAsync("Access Denied.\nOpen access from the internet detected. Please enable " +
                    "basic authentication or disable this warning by setting the environment variable \"BREC_HTTP_OPEN_ACCESS\".\n" +
                    "检测到非局域网无密码访问。请设置用户名密码或通过设置环境变量 \"BREC_HTTP_OPEN_ACCESS\" 禁用此警告。\n" +
                    "录播姬 BililiveRecorder " + GitVersionInformation.FullSemVer + "\n");
                }
            }
            else
            {
                return this.next(context);
            }
        }

        private static bool sourceIpNotLan(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress;
            if (ip is null) return true;
            return !isLocalIpv4Address(ip) && !ip.IsIPv6LinkLocal && !ip.IsIPv6UniqueLocal;
        }

        private static bool isLocalIpv4Address(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                return false;

            var bytes = ip.GetAddressBytes();
            if (bytes.Length != 4)
                return false;
            switch (bytes[0])
            {
                case 10: // 10.0.0.0/8
                    return true;
                case 127: // 127.0.0.0/8
                    return true;
                case 172: // 172.16.0.0/12
                    return bytes[1] >= 16 && bytes[1] <= 31;
                case 192: // 192.168.0.0/16
                    return bytes[1] == 168;
                default:
                    return false;
            }
        }

        private static bool haveReverseProxyHeaders(HttpContext context)
        {
            return
                context.Request.Headers.ContainsKey("X-Real-IP") ||
                context.Request.Headers.ContainsKey("X-Forwarded-For") ||
                context.Request.Headers.ContainsKey("X-Forwarded-Host") ||
                context.Request.Headers.ContainsKey("Via");
        }

        private static bool haveCustomHostValue(HttpContext context)
        {
            // check if the host header is set to a custom value such as a domain name
            if (IPAddress.TryParse(context.Request.Host.Host, out var ip))
            {
                // the host header is an IP address
                // check if the IP address matches the server's IP address
                return ip.Equals(context.Connection.LocalIpAddress);
            }
            // the host header is not an IP address
            return true;
        }
    }
}
