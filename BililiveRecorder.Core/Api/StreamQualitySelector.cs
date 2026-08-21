using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BililiveRecorder.Core.Api
{
    internal static class StreamQualitySelector
    {
        internal static readonly char[] QnParseSeparator = new[] { ',', '，', '、', ' ' };

        internal static IReadOnlyList<StreamCodecQn> ParseAllowedQn(string? allowedQn)
        {
            if (string.IsNullOrWhiteSpace(allowedQn)) return Array.Empty<StreamCodecQn>();

            var qns = allowedQn!.Split(QnParseSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(static x =>
                {
                    if (int.TryParse(x, out var num))
                    {
                        return new StreamCodecQn
                        {
                            Qn = num,
                            Codec = StreamCodec.AVC
                        };
                    }
                    else if (x.StartsWith("avc", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(x[3..], out num))
                        {
                            return new StreamCodecQn
                            {
                                Qn = num,
                                Codec = StreamCodec.AVC
                            };
                        }
                    }
                    else if (x.StartsWith("hevc", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(x[4..], out num))
                        {
                            return new StreamCodecQn
                            {
                                Qn = num,
                                Codec = StreamCodec.HEVC
                            };
                        }
                    }

                    // invalid
                    return new StreamCodecQn
                    {
                        Qn = -1,
                        Codec = StreamCodec.AVC
                    };
                })
                .Where(x => x.Qn >= 0)
                .ToList();

            return qns;
        }

        /// <summary>
        /// 查询当前可用的最优画质。按 allowedQn 的先后顺序（列表顺序即优先级）返回第一个可用的画质。
        /// 无匹配时抛出 NoMatchingQnValueException。
        /// </summary>
        internal static async Task<StreamCodecQn> SelectBestAvailableAsync(IApiClient apiClient, IReadOnlyList<StreamCodecQn> allowedQn, int roomid)
        {
            const int DefaultQn = 10000;
            var codecItems = await apiClient.GetCodecItemInStreamUrlAsync(roomid: roomid, qn: DefaultQn).ConfigureAwait(false);

            var allAvailableCodecQn = new List<StreamCodecQn>();

            if (codecItems.avc is not null)
            {
                allAvailableCodecQn.AddRange(codecItems.avc.AcceptQn.Select(x => new StreamCodecQn
                {
                    Codec = StreamCodec.AVC,
                    Qn = x
                }));
            }
            if (codecItems.hevc is not null)
            {
                allAvailableCodecQn.AddRange(codecItems.hevc.AcceptQn.Select(x => new StreamCodecQn
                {
                    Codec = StreamCodec.HEVC,
                    Qn = x
                }));
            }

            foreach (var qn in allowedQn)
            {
                if (allAvailableCodecQn.Contains(qn))
                {
                    return qn;
                }
            }

            throw new NoMatchingQnValueException();
        }

        /// <summary>
        /// 返回 qn 在 allowedQn 列表中的下标；不存在返回 -1。列表顺序即优先级，下标越小优先级越高。
        /// </summary>
        internal static int IndexOfQn(IReadOnlyList<StreamCodecQn> list, StreamCodecQn qn)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Codec == qn.Codec && list[i].Qn == qn.Qn)
                    return i;
            }

            return -1;
        }
    }
}
