using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api.Model
{
    internal class DanmuInfo
    {
        [JsonProperty("host_list")]
        public HostListItem[] HostList { get; set; } = Array.Empty<HostListItem>();

        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        public class HostListItem
        {
            [JsonProperty("host")]
            public string Host { get; set; } = string.Empty;

            [JsonProperty("port")]
            public int Port { get; set; }
        }
    }
}
