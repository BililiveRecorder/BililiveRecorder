using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Grouping.Rules
{
    public class DataGroupingRule : IGroupingRule
    {
        // Threshold for accumulated data size before forcing group completion
        // Set to 100MB which is approximately 1 minute of data for high bitrate streams
        // Calculation: ~1 minute × 15 Mbps average ≈ 112.5 MiB
        // This prevents memory bloat for audio-only streams or streams without I-frames
        private const ulong MaxAccumulatedSize = 100 * 1024 * 1024; // 100 MB

        public bool CanStartWith(Tag tag) => tag.IsData();

        public bool CanAppendWith(Tag tag, List<Tag> tags)
        {
            // Check if we've accumulated too much data without finding an I-frame
            // This handles audio-only streams or streams with missing video track
            // Note: This has O(n) complexity per call, resulting in O(n²) total complexity
            // for group building. However, groups are bounded by:
            // 1. MaxAccumulatedSize limit (100MB)
            // 2. Timestamp check limit (25 seconds)
            // 3. Natural I-frame occurrence in normal streams
            // So in practice n is small, making this acceptable.
            ulong accumulatedSize = 0;
            for (var i = 0; i < tags.Count; i++)
            {
                // Explicit cast from uint to ulong to prevent any potential overflow issues
                accumulatedSize += (ulong)tags[i].Size;
                if (accumulatedSize >= MaxAccumulatedSize)
                {
                    // Force group completion - don't accept any more tags
                    return false;
                }
            }

            return
                // Tag 是非关键帧数据，并且与前一个 Tag 的时间戳差距不超过 25 秒
                (tag.IsNonKeyframeData() && (tags.LastOrDefault() is not Tag lastTag || (tag.Timestamp - lastTag.Timestamp < 24999)))
                // 或：是关键帧，并且之前只有音频数据
                || (tag.Type == TagType.Video && tag.IsKeyframeData() && tags.TrueForAll(x => x.Type == TagType.Audio))
                // 或：是音频头，并且之前未出现过音频数据
                || (tag.Type == TagType.Audio && tag.IsHeader() && tags.TrueForAll(x => x.Type != TagType.Audio || x.Flag == TagFlag.Header));
            // || (tag.IsKeyframeData() && tags.All(x => x.IsNonKeyframeData()))
        }

        public PipelineAction CreatePipelineAction(List<Tag> tags) => new PipelineDataAction(tags);
    }
}
