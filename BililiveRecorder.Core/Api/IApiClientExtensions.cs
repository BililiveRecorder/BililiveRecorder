using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BililiveRecorder.Core.Api.Model.RoomPlayInfo;

namespace BililiveRecorder.Core.Api
{
    /// <summary>
    /// 直播流地址中可用的编码流信息。
    /// </summary>
    internal sealed class StreamUrlCodecs
    {
        /// <summary>FLV (http_stream/flv) AVC 编码流</summary>
        public CodecItem? FlvAvc { get; init; }

        /// <summary>FLV (http_stream/flv) HEVC 编码流</summary>
        public CodecItem? FlvHevc { get; init; }

        /// <summary>HLS (http_hls) AVC 编码流，按格式分组（fmp4/ts）</summary>
        public IReadOnlyList<HlsCodecItem> HlsAvc { get; init; } = Array.Empty<HlsCodecItem>();

        /// <summary>HLS (http_hls) HEVC 编码流，按格式分组（fmp4/ts）</summary>
        public IReadOnlyList<HlsCodecItem> HlsHevc { get; init; } = Array.Empty<HlsCodecItem>();
    }

    /// <summary>
    /// HLS 编码流条目：封装格式（fmp4/ts）+ 编码流信息。
    /// </summary>
    internal readonly record struct HlsCodecItem(string FormatName, CodecItem Item);

    internal static class IApiClientExtensions
    {
        /// <summary>
        /// 获取直播流地址响应中的编码流列表（同时包含 FLV 和 HLS）。
        /// </summary>
        /// <exception cref="Exception">playurl is null</exception>
        public static async Task<StreamUrlCodecs> GetStreamUrlCodecsAsync(this IApiClient apiClient, int roomid, int qn)
        {
            var apiResp = await apiClient.GetStreamUrlAsync(roomid: roomid, qn: qn).ConfigureAwait(false);
            var url_data = apiResp?.Data?.PlayurlInfo?.Playurl?.Streams;

            if (url_data is null) throw new Exception("playurl is null");

            CodecItem? flvAvc = null;
            CodecItem? flvHevc = null;
            List<HlsCodecItem>? hlsAvc = null;
            List<HlsCodecItem>? hlsHevc = null;

            foreach (var stream in url_data)
            {
                if (stream.ProtocolName == "http_stream")
                {
                    var flv = stream.Formats?.FirstOrDefault(x => x.FormatName == "flv");
                    if (flv?.Codecs is { Length: > 0 })
                    {
                        flvAvc ??= flv.Codecs.FirstOrDefault(x => x.CodecName == "avc");
                        flvHevc ??= flv.Codecs.FirstOrDefault(x => x.CodecName == "hevc");
                    }
                }
                else if (stream.ProtocolName == "http_hls")
                {
                    foreach (var format in stream.Formats ?? Array.Empty<FormatItem>())
                    {
                        // 只关心可以直接顺序写入文件的封装格式
                        if (format.FormatName is not ("fmp4" or "ts"))
                            continue;

                        foreach (var codec in format.Codecs ?? Array.Empty<CodecItem>())
                        {
                            var entry = new HlsCodecItem(format.FormatName, codec);
                            switch (codec.CodecName)
                            {
                                case "avc":
                                    (hlsAvc ??= new List<HlsCodecItem>()).Add(entry);
                                    break;
                                case "hevc":
                                    (hlsHevc ??= new List<HlsCodecItem>()).Add(entry);
                                    break;
                            }
                        }
                    }
                }
            }

            return new StreamUrlCodecs
            {
                FlvAvc = flvAvc,
                FlvHevc = flvHevc,
                HlsAvc = hlsAvc ?? (IReadOnlyList<HlsCodecItem>)Array.Empty<HlsCodecItem>(),
                HlsHevc = hlsHevc ?? (IReadOnlyList<HlsCodecItem>)Array.Empty<HlsCodecItem>(),
            };
        }

        /// <summary>
        /// 兼容旧调用：仅返回 FLV 编码流（avc, hevc）。
        /// </summary>
        /// <exception cref="Exception">playurl is null / no supported stream</exception>
        public static async Task<(CodecItem? avc, CodecItem? hevc)> GetCodecItemInStreamUrlAsync(this IApiClient apiClient, int roomid, int qn)
        {
            var codecs = await apiClient.GetStreamUrlCodecsAsync(roomid, qn).ConfigureAwait(false);
            return (codecs.FlvAvc, codecs.FlvHevc);
        }
    }
}
