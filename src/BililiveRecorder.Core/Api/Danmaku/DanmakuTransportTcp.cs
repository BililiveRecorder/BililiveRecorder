using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Nerdbank.Streams;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuTransportTcp : IDanmakuTransport
    {
        private Stream? stream;

        public DanmakuTransportTcp()
        {
        }

        public async Task<PipeReader> ConnectAsync(string host, int port, CancellationToken cancellationToken)
        {
            if (this.stream is not null)
                throw new InvalidOperationException("Tcp socket is connected.");

            var tcp = new TcpClient();
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
