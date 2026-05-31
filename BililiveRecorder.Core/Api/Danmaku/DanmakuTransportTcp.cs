using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Http;
using Nerdbank.Streams;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuTransportTcp : IDanmakuTransport
    {
        private Stream? stream;
        private readonly string? bindAddress;

        public DanmakuTransportTcp(string? bindAddress = null)
        {
            this.bindAddress = bindAddress;
        }

        public async Task<PipeReader> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            if (this.stream is not null)
                throw new InvalidOperationException("Tcp socket is connected.");

            var tcp = new TcpClient();

            if (!string.IsNullOrWhiteSpace(this.bindAddress))
            {
                var localAddress = HttpClientWithBindAddress.ResolveBindAddress(this.bindAddress);
                if (localAddress is not null)
                {
                    tcp.Client.Bind(new IPEndPoint(localAddress, 0));
                }
            }

            await tcp.ConnectAsync(host, port).ConfigureAwait(false);

            var networkStream = tcp.GetStream();
            this.stream = networkStream;
            return networkStream.UsePipeReader();
        }

        public void Dispose() => this.stream?.Dispose();

        public async Task SendAsync(byte[] buffer, int offset, int count)
        {
            if (this.stream is not { } s)
                return;

            await s.WriteAsync(buffer, offset, count).ConfigureAwait(false);
            await s.FlushAsync().ConfigureAwait(false);
        }
    }
}
