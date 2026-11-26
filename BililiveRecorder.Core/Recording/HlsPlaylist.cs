using System;
using System.Collections.Generic;
using System.Globalization;

namespace BililiveRecorder.Core.Recording
{
    /// <summary>
    /// HLS m3u8 播放列表（仅支持媒体播放列表，分片为顺序下载的 TS/fMP4）。
    /// </summary>
    internal sealed class HlsPlaylist
    {
        public long MediaSequence { get; private set; }
        public double TargetDuration { get; private set; } = 2;
        public bool EndList { get; private set; }
        public bool HasDiscontinuity { get; private set; }
        public string? MapUri { get; private set; }
        public List<HlsSegment> Segments { get; } = new List<HlsSegment>();

        /// <summary>
        /// 是否包含加密信息（EXT-X-KEY METHOD != NONE）。Bilibili 公开直播不加密。
        /// </summary>
        public bool IsEncrypted { get; private set; }

        public readonly record struct HlsSegment(long Sequence, double Duration, string Uri);

        /// <summary>
        /// 解析 m3u8 文本。若为主播放列表（包含 EXT-X-STREAM-INF）则抛出异常——目前不支持。
        /// </summary>
        public static HlsPlaylist Parse(string text)
        {
            var playlist = new HlsPlaylist();
            var pendingDuration = -1d;
            var pendingDiscontinuity = false;
            var seq = 0L;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.Trim('\r', ' ', '\t');
                if (line.Length == 0)
                    continue;

                if (line[0] == '#')
                {
                    if (line.StartsWith("#EXTM3U", StringComparison.Ordinal))
                        continue;

                    if (line.StartsWith("#EXT-X-MEDIA-SEQUENCE:", StringComparison.Ordinal))
                    {
                        _ = long.TryParse(line["#EXT-X-MEDIA-SEQUENCE:".Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out seq);
                        playlist.MediaSequence = seq;
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-TARGETDURATION:", StringComparison.Ordinal))
                    {
                        _ = double.TryParse(line["#EXT-X-TARGETDURATION:".Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var td);
                        if (td > 0) playlist.TargetDuration = td;
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-ENDLIST", StringComparison.Ordinal))
                    {
                        playlist.EndList = true;
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-DISCONTINUITY", StringComparison.Ordinal))
                    {
                        pendingDiscontinuity = true;
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-MAP:", StringComparison.Ordinal))
                    {
                        // #EXT-X-MAP:URI="init.mp4"
                        var uriIdx = line.IndexOf("URI=\"", StringComparison.Ordinal);
                        if (uriIdx >= 0)
                        {
                            var start = uriIdx + 5;
                            var end = line.IndexOf('"', start);
                            if (end > start)
                                playlist.MapUri = line[start..end];
                        }
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-KEY:", StringComparison.Ordinal))
                    {
                        // METHOD=NONE 表示未加密；其他情况标记为加密
                        if (line.IndexOf("METHOD=NONE", StringComparison.OrdinalIgnoreCase) < 0)
                            playlist.IsEncrypted = true;
                        continue;
                    }

                    if (line.StartsWith("#EXT-X-STREAM-INF", StringComparison.Ordinal))
                        throw new InvalidOperationException("不支持的 HLS 主播放列表（包含多码率子流）");

                    if (line.StartsWith("#EXTINF:", StringComparison.Ordinal))
                    {
                        var comma = line.IndexOf(',');
                        var durStr = comma >= 0 ? line["#EXTINF:".Length..comma] : line["#EXTINF:".Length..];
                        _ = double.TryParse(durStr, NumberStyles.Float, CultureInfo.InvariantCulture, out pendingDuration);
                        continue;
                    }

                    // 其他 #EXT-* 标签忽略
                    continue;
                }

                // 分片 URI 行
                if (pendingDuration >= 0)
                {
                    playlist.Segments.Add(new HlsSegment(seq, pendingDuration, line));
                    seq++;
                    pendingDuration = -1;
                    if (pendingDiscontinuity)
                    {
                        playlist.HasDiscontinuity = true;
                        pendingDiscontinuity = false;
                    }
                }
            }

            return playlist;
        }
    }
}
