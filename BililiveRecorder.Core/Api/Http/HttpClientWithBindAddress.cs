using System;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core.Api.Http
{
    internal static class HttpClientWithBindAddress
    {
        /// <summary>
        /// Creates an HttpMessageHandler with a ConnectCallback that binds to the specified local address or interface.
        /// </summary>
        /// <param name="bindAddress">The local IP address or network interface name to bind to. If null or empty, no binding is performed.</param>
        /// <param name="useProxy">Whether to use system proxy.</param>
        /// <param name="allowAutoRedirect">Whether to allow auto redirect. Default is true.</param>
        /// <returns>A configured HttpMessageHandler.</returns>
        public static HttpMessageHandler CreateHandler(string? bindAddress, bool useProxy, bool allowAutoRedirect = true)
        {
#if NET8_0_OR_GREATER
            var handler = new SocketsHttpHandler
            {
                UseProxy = useProxy,
                UseCookies = false,
                AllowAutoRedirect = allowAutoRedirect,
            };

            if (!string.IsNullOrWhiteSpace(bindAddress))
            {
                var localAddress = ResolveBindAddress(bindAddress);
                if (localAddress is not null)
                {
                    handler.ConnectCallback = (context, cancellationToken) =>
                        ConnectWithBindAsync(context, localAddress, cancellationToken);
                }
            }

            return handler;
#else
            return new HttpClientHandler
            {
                UseProxy = useProxy,
                UseCookies = false,
                AllowAutoRedirect = allowAutoRedirect,
            };
#endif
        }

        /// <summary>
        /// Resolves a bind address string to an IPAddress.
        /// The string can be an IP address or a network interface name.
        /// </summary>
        internal static IPAddress? ResolveBindAddress(string? bindAddress)
        {
            if (string.IsNullOrWhiteSpace(bindAddress))
                return null;

            // Try parse as IP address first
            if (IPAddress.TryParse(bindAddress, out var ipAddress))
                return ipAddress;

            // Try to resolve as network interface name
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var ni in interfaces)
                {
                    if (string.Equals(ni.Name, bindAddress, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(ni.Description, bindAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        var properties = ni.GetIPProperties();
                        foreach (var addr in properties.UnicastAddresses)
                        {
                            // Prefer IPv4 address
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                                return addr.Address;
                        }
                        // Fall back to IPv6
                        foreach (var addr in properties.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                                return addr.Address;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors resolving interface name
            }

            return null;
        }

#if NET8_0_OR_GREATER
        private static async ValueTask<System.IO.Stream> ConnectWithBindAsync(
            SocketsHttpConnectionContext context,
            IPAddress localAddress,
            CancellationToken cancellationToken)
        {
            var socket = new Socket(localAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                socket.Bind(new IPEndPoint(localAddress, 0));
                await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
#endif
    }
}
