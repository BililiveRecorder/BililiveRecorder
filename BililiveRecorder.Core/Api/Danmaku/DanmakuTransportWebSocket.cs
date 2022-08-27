using System;
using System.IO.Pipelines;
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
        private static readonly bool isDotNetFramework = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.Ordinal);

        private readonly ClientWebSocket socket;

        protected virtual string Scheme => "ws";

        public DanmakuTransportWebSocket()
        {
            this.socket = new ClientWebSocket();
            this.socket.Options.UseDefaultCredentials = false;
            this.socket.Options.Credentials = null;
            this.socket.Options.Proxy = null;
            this.socket.Options.Cookies = null;

            this.socket.Options.SetRequestHeader("Origin", HttpApiClient.HttpHeaderOrigin);

            if (!isDotNetFramework)
                this.socket.Options.SetRequestHeader("User-Agent", HttpApiClient.HttpHeaderUserAgent);
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
