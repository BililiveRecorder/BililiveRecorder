using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal static class NetworkChangeDetector
    {
        private static bool enabled = false;
        private static readonly ILogger logger = Log.ForContext(typeof(NetworkChangeDetector));

        private static readonly object debounceLock = new();
        private static readonly TimeSpan debounceDelay = TimeSpan.FromSeconds(15);
        private static CancellationTokenSource? debounceTokenSource = null;

        internal static void Enable()
        {
            if (!enabled)
            {
                enabled = true;

                logger.Debug("NetworkChangeDetector Enabled");

                NetworkChange.NetworkAddressChanged += (sender, args) =>
                {
                    logger.Debug("NetworkChange.NetworkAddressChanged");
                    LogNetworkInfo();
                };

                LogNetworkInfo();
            }
        }

        private static void LogNetworkInfo()
        {
            lock (debounceLock)
            {
                debounceTokenSource?.Cancel();
                debounceTokenSource = new CancellationTokenSource();
                _ = Task.Delay(debounceDelay, debounceTokenSource.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        try
                        {
                            LogNetworkInfoWithoutDebounce();
                        }
                        catch (Exception ex)
                        {
                            logger.Warning(ex, "检测网络状态时发生了错误");
                        }
                    }
                }, TaskScheduler.Default);
            }
        }

        private static void LogNetworkInfoWithoutDebounce()
        {
            var localV4 = ProbeLocalAddress(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53));
            var localV6 = ProbeLocalAddress(new IPEndPoint(IPAddress.Parse("2001:4860:4860::8888"), 53));

            var interfaces = NetworkInterface.GetAllNetworkInterfaces().Select(x => new NetInterface
            {
                Name = x.Name,
                Description = x.Description,
                NetworkInterfaceType = x.NetworkInterfaceType,
                OperationalStatus = x.OperationalStatus,
                Speed = x.Speed,
                Addresses = x.GetIPProperties().UnicastAddresses.Select(x => x.Address).ToArray()
            }).ToArray();

            var info = new NetInfo
            {
                LocalIpv4 = MaskAddressForLogging(localV4),
                LocalIpv6 = MaskAddressForLogging(localV6),
                Interfaces = interfaces,
                IsIpv6Enabled = localV6 is not null,
                IsWifiEnabled = interfaces.Any(x => x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            };

            for (var i = 0; i < interfaces.Length; i++)
            {
                var ni = interfaces[i];
                if (ni.Addresses.Contains(localV4))
                {
                    ni.Flags |= NetInterfaceFlags.DefaultIpv4Interface;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        info.IsWifiUsed = true;
                }
                if (ni.Addresses.Contains(localV6))
                {
                    ni.Flags |= NetInterfaceFlags.DefaultIpv6Interface;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        info.IsWifiUsed = true;
                }
            }

            // Data collection completed, masking ips before logging

            for (var i = 0; i < interfaces.Length; i++)
            {
                var ni = interfaces[i];
                ni.Addresses = ni.Addresses.Select(x => MaskAddressForLogging(x)!).ToArray();
            }

            // Log

            logger.Debug("Network Info: {@NetworkInfo}", info);

            if (info.IsWifiUsed)
            {
                logger.Warning("检测到当前使用的是WiFi网络，可能不稳定，容易造成录播断开等问题。强烈建议使用有线网络录播。");
            }
        }

        internal static IPAddress? MaskAddressForLogging(IPAddress? address)
        {
            switch ((address?.AddressFamily) ?? AddressFamily.Unknown)
            {
                case AddressFamily.InterNetwork:
                    {
                        var bytes = address!.GetAddressBytes();
                        bytes[3] = 0;
                        return new IPAddress(bytes);
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        var bytes = address!.GetAddressBytes();
                        if (address.IsIPv4MappedToIPv6)
                        {
                            bytes[15] = 0;
                        }
                        else
                        {
                            bytes[8] = 0;
                            bytes[9] = 0;
                            bytes[10] = 0;
                            bytes[11] = 0;
                            bytes[12] = 0;
                            bytes[13] = 0;
                            bytes[14] = 0;
                            bytes[15] = 0;
                        }
                        return new IPAddress(bytes);
                    }
                default:
                    return address;
            }
        }

        private static IPAddress? ProbeLocalAddress(IPEndPoint remote)
        {
            try
            {
                using var socket = new Socket(remote.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect(remote);
                return (socket.LocalEndPoint as IPEndPoint)?.Address;
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "Local address probing failed with remote address {RemoteAddress}", remote.Address);
                return null;
            }
        }

        public class NetInfo
        {
            /// <summary>
            /// Does wireless network interface exist
            /// </summary>
            public bool IsWifiEnabled { get; set; }

            /// <summary>
            /// Is wireless network being used
            /// </summary>
            public bool IsWifiUsed { get; set; }

            public bool IsIpv6Enabled { get; set; }

            public IPAddress? LocalIpv4 { get; set; }
            public IPAddress? LocalIpv6 { get; set; }

            public NetInterface[] Interfaces { get; set; } = Array.Empty<NetInterface>();
        }

        public class NetInterface
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;

            public NetInterfaceFlags Flags { get; set; }

            public NetworkInterfaceType NetworkInterfaceType { get; set; }
            public OperationalStatus OperationalStatus { get; set; }
            public long Speed { get; set; }

            public IPAddress[] Addresses { get; set; } = Array.Empty<IPAddress>();
        }

        [Flags]
        public enum NetInterfaceFlags
        {
            None = 0,
            DefaultIpv4Interface = 1 << 0,
            DefaultIpv6Interface = 1 << 1,
        }
    }
}
