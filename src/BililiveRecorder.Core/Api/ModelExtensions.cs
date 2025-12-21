using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config;

namespace BililiveRecorder.Core.Api
{
    internal static class ModelExtensions
    {
        private static readonly Random random = new Random();

        private const string DefaultServerHost = "broadcastlv.chat.bilibili.com";

        private static readonly DanmakuServerInfo[] DefaultServers = new[] {
            new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Tcp, Host = DefaultServerHost, Port = 2243 },
            new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Ws, Host = DefaultServerHost, Port = 2244 },
            new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Wss, Host = DefaultServerHost, Port = 443 },
        };

        public static DanmakuServerInfo SelectDanmakuServer(this DanmuInfo danmuInfo, DanmakuTransportMode transportMode)
        {
            static IEnumerable<DanmakuServerInfo> SelectServerInfo(DanmuInfo.HostListItem x)
            {
                yield return new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Tcp, Host = x.Host, Port = x.Port };
                yield return new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Ws, Host = x.Host, Port = x.WsPort };
                yield return new DanmakuServerInfo { TransportMode = DanmakuTransportMode.Wss, Host = x.Host, Port = x.WssPort };
            }

            var list = danmuInfo.HostList.Where(x => !string.IsNullOrWhiteSpace(x.Host) && x.Host != DefaultServerHost)
                                         .SelectMany(SelectServerInfo)
                                         .Where(x => x.Port > 0)
                                         .Where(x => transportMode == DanmakuTransportMode.Random || transportMode == x.TransportMode)
                                         .ToArray();
            if (list.Length > 0)
            {
                var result = list[random.Next(list.Length)];
                result.Token = danmuInfo.Token;
                return result;
            }
            else
            {
                return DefaultServers[random.Next(DefaultServers.Length)];
            }
        }

        internal struct DanmakuServerInfo
        {
            internal DanmakuTransportMode TransportMode;
            internal string Host;
            internal int Port;
            internal string Token;
        }
    }
}
