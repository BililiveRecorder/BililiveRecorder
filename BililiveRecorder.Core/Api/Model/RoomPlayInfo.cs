using System;
using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api.Model
{
    internal class RoomPlayInfo
    {
        [JsonProperty("live_status")]
        public int LiveStatus { get; set; }

        [JsonProperty("encrypted")]
        public bool Encrypted { get; set; }

        [JsonProperty("playurl_info")]
        public PlayurlInfoClass? PlayurlInfo { get; set; }

        public class PlayurlInfoClass
        {
            [JsonProperty("playurl")]
            public PlayurlClass? Playurl { get; set; }
        }

        public class PlayurlClass
        {
            [JsonProperty("stream")]
            public StreamItem[]? Streams { get; set; } = Array.Empty<StreamItem>();
        }

        public class StreamItem
        {
            [JsonProperty("protocol_name")]
            public string ProtocolName { get; set; } = string.Empty;

            [JsonProperty("format")]
            public FormatItem[]? Formats { get; set; } = Array.Empty<FormatItem>();
        }

        public class FormatItem
        {
            [JsonProperty("format_name")]
            public string FormatName { get; set; } = string.Empty;

            [JsonProperty("codec")]
            public CodecItem[]? Codecs { get; set; } = Array.Empty<CodecItem>();
        }

        public class CodecItem
        {
            [JsonProperty("codec_name")]
            public string CodecName { get; set; } = string.Empty;

            [JsonProperty("base_url")]
            public string BaseUrl { get; set; } = string.Empty;

            [JsonProperty("current_qn")]
            public int CurrentQn { get; set; }

            [JsonProperty("accept_qn")]
            public int[] AcceptQn { get; set; } = Array.Empty<int>();

            [JsonProperty("url_info")]
            public UrlInfoItem[]? UrlInfos { get; set; } = Array.Empty<UrlInfoItem>();
        }

        public class UrlInfoItem
        {
            [JsonProperty("host")]
            public string Host { get; set; } = string.Empty;

            [JsonProperty("extra")]
            public string Extra { get; set; } = string.Empty;
        }
    }
}
