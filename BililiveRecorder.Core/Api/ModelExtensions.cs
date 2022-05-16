using System;
using System.Linq;
using BililiveRecorder.Core.Api.Model;

namespace BililiveRecorder.Core.Api
{
    internal static class ModelExtensions
    {
        private static readonly Random random = new Random();

        public static void ChooseOne(this DanmuInfo danmuInfo, out string host, out int port, out string token)
        {
            const string DefaultServerHost = "broadcastlv.chat.bilibili.com";
            const int DefaultServerPort = 2243;

            token = danmuInfo.Token;

            var list = danmuInfo.HostList.Where(x => !string.IsNullOrWhiteSpace(x.Host) && x.Host != DefaultServerHost && x.Port > 0).ToArray();
            if (list.Length > 0)
            {
                var result = list[random.Next(list.Length)];
                host = result.Host;
                port = result.Port;
            }
            else
            {
                host = DefaultServerHost;
                port = DefaultServerPort;
            }
        }
    }
}
