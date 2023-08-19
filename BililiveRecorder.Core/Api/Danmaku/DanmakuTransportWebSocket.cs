using System;
using System.Collections;
using System.IO.Pipelines;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Http;
using Nerdbank.Streams;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuTransportWebSocket : IDanmakuTransport
    {
        private readonly ClientWebSocket socket;

        protected virtual string Scheme => "ws";

        static DanmakuTransportWebSocket()
        {
            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal))
                return;

            var headerInfoTable = typeof(WebHeaderCollection).Assembly.GetType("System.Net.HeaderInfoTable", false);
            if (headerInfoTable is null) return;

            var headerHashTable = headerInfoTable.GetField("HeaderHashTable", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (headerHashTable is null) return;

            if (headerHashTable.GetValue(null) is not Hashtable table) return;

            var info = table["User-Agent"];
            if (info is null) return;

            var isRequestRestrictedProperty = info.GetType().GetField("IsRequestRestricted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (isRequestRestrictedProperty is null) return;

            isRequestRestrictedProperty.SetValue(info, false);
        }

        public DanmakuTransportWebSocket()
        {
            this.socket = new ClientWebSocket();
            var options = this.socket.Options;
            options.UseDefaultCredentials = false;
            options.Credentials = null;
            options.Proxy = null;
            options.Cookies = null;
            options.SetRequestHeader("Origin", HttpApiClient.HttpHeaderOrigin);
            options.SetRequestHeader("Accept-Language", HttpApiClient.HttpHeaderAcceptLanguage);
            options.SetRequestHeader("User-Agent", HttpApiClient.HttpHeaderUserAgent);
        }

        public async Task<PipeReader> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            var b = new UriBuilder(this.Scheme, host, port, "/sub");

            // 连接超时 10 秒
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            await this.socket.ConnectAsync(b.Uri, cts.Token).ConfigureAwait(false);
            return this.socket.UsePipeReader();
        }

        public async Task SendAsync(byte[] buffer, int offset, int count)
            => await this.socket.SendAsync(new ArraySegment<byte>(buffer, offset, count), WebSocketMessageType.Binary, true, default).ConfigureAwait(false);

        public void Dispose() => this.socket.Dispose();
    }
}
