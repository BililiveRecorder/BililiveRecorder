using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Http;
using BililiveRecorder.Core.Config;
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

        public async Task<PipeReader> ConnectAsync(string host, int port, AllowedAddressFamily allowedAddressFamily, CancellationToken cancellationToken)
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

            if (allowedAddressFamily == AllowedAddressFamily.System)
            {
                await tcp.ConnectAsync(host, port).ConfigureAwait(false);
            }
            else
            {
                var ips = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);

                var filtered = ips.Where(x => allowedAddressFamily switch
                {
                    AllowedAddressFamily.Ipv4 => x.AddressFamily == AddressFamily.InterNetwork,
                    AllowedAddressFamily.Ipv6 => x.AddressFamily == AddressFamily.InterNetworkV6,
                    AllowedAddressFamily.Any => true,
                    _ => false
                }).ToArray();

                if (filtered.Length == 0)
                    throw new InvalidOperationException("DNS did not return any IP addresses matching the allowed address family.");

                var selected = filtered[new Random().Next(filtered.Length)];
                await tcp.ConnectAsync(selected, port).ConfigureAwait(false);
            }

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
