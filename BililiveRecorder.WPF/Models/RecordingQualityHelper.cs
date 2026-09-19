using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    public enum CodecPreference
    {
        AvcFirst,
        HevcFirst,
        AvcOnly,
        HevcOnly,
    }

    public class CodecPreferenceItem
    {
        public CodecPreference Value { get; }
        public string DisplayName { get; }

        public CodecPreferenceItem(CodecPreference value, string displayName)
        {
            this.Value = value;
            this.DisplayName = displayName;
        }

        public override string ToString() => this.DisplayName;
    }

    public class QualityItem
    {
        public int Qn { get; }
        public string DisplayName { get; }

        public QualityItem(int qn, string displayName)
        {
            this.Qn = qn;
            this.DisplayName = displayName;
        }

        public override string ToString() => this.DisplayName;
    }

    public static class RecordingQualityHelper
    {
        public static readonly CodecPreferenceItem[] CodecPreferenceItems = new[]
        {
            new CodecPreferenceItem(CodecPreference.AvcFirst, "AVC 优先 (先 H.264, 后 H.265)"),
            new CodecPreferenceItem(CodecPreference.HevcFirst, "HEVC 优先 (先 H.265, 后 H.264)"),
            new CodecPreferenceItem(CodecPreference.AvcOnly, "仅 AVC (H.264)"),
            new CodecPreferenceItem(CodecPreference.HevcOnly, "仅 HEVC (H.265)"),
        };

        // Quality levels in descending order
        private static readonly int[] QualityLevels = { 30000, 20000, 10000, 400, 250, 150, 80 };

        public static readonly QualityItem[] QualityItems = new[]
        {
            new QualityItem(30000, "杜比 (30000)"),
            new QualityItem(20000, "4K (20000)"),
            new QualityItem(10000, "原画 (10000)"),
            new QualityItem(400, "蓝光 (400)"),
            new QualityItem(250, "超清 (250)"),
            new QualityItem(150, "高清 (150)"),
            new QualityItem(80, "流畅 (80)"),
        };

        /// <summary>
        /// Generate a RecordingQuality string from dropdown selections.
        /// Includes all quality levels from the selected max downward for automatic fallback.
        /// </summary>
        public static string GenerateQualityString(CodecPreference codec, int maxQn)
        {
            var qualitiesFromMax = QualityLevels.Where(q => q <= maxQn).ToArray();
            if (qualitiesFromMax.Length == 0)
                qualitiesFromMax = new[] { maxQn };

            var parts = new List<string>();

            switch (codec)
            {
                case CodecPreference.AvcOnly:
                    foreach (var q in qualitiesFromMax)
                        parts.Add($"avc{q}");
                    break;

                case CodecPreference.HevcOnly:
                    foreach (var q in qualitiesFromMax)
                        parts.Add($"hevc{q}");
                    break;

                case CodecPreference.AvcFirst:
                    foreach (var q in qualitiesFromMax)
                    {
                        parts.Add($"avc{q}");
                        parts.Add($"hevc{q}");
                    }
                    break;

                case CodecPreference.HevcFirst:
                    foreach (var q in qualitiesFromMax)
                    {
                        parts.Add($"hevc{q}");
                        parts.Add($"avc{q}");
                    }
                    break;
            }

            return string.Join(",", parts);
        }

        /// <summary>
        /// Parse RecordingQuality string to determine codec preference and max quality.
        /// Returns false if parsing cannot determine values (e.g., user entered custom string).
        /// </summary>
        public static bool TryParseQualityString(string? qualityString, out CodecPreference codec, out int maxQn)
        {
            codec = CodecPreference.AvcFirst;
            maxQn = 10000;

            if (string.IsNullOrWhiteSpace(qualityString))
                return false;

            var separators = new[] { ',', '，', '、', ' ' };
            var entries = qualityString!.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (entries.Length == 0)
                return false;

            var parsed = new List<(bool isAvc, int qn)>();
            foreach (var entry in entries)
            {
                var e = entry.Trim();
                if (int.TryParse(e, out var num))
                {
                    parsed.Add((true, num));
                }
                else if (e.StartsWith("avc", StringComparison.OrdinalIgnoreCase) && int.TryParse(e.Substring(3), out num))
                {
                    parsed.Add((true, num));
                }
                else if (e.StartsWith("hevc", StringComparison.OrdinalIgnoreCase) && int.TryParse(e.Substring(4), out num))
                {
                    parsed.Add((false, num));
                }
                else
                {
                    // Unknown token, treat as custom string
                    return false;
                }
            }

            if (parsed.Count == 0 || parsed.Count != entries.Length)
                return false;

            // Determine max quality from first entry
            maxQn = parsed[0].qn;

            // Determine codec preference from patterns
            var hasAvc = parsed.Any(p => p.isAvc);
            var hasHevc = parsed.Any(p => !p.isAvc);

            if (hasAvc && !hasHevc)
            {
                codec = CodecPreference.AvcOnly;
            }
            else if (!hasAvc && hasHevc)
            {
                codec = CodecPreference.HevcOnly;
            }
            else if (hasAvc && hasHevc)
            {
                // Check first entry to determine priority
                codec = parsed[0].isAvc ? CodecPreference.AvcFirst : CodecPreference.HevcFirst;
            }

            // Validate that maxQn is a known quality level
            if (!QualityLevels.Contains(maxQn))
            {
                // Find nearest known quality level that is <= maxQn (do not exceed requested max)
                var currentMax = maxQn;
                var nearest = QualityLevels.Where(q => q <= currentMax).FirstOrDefault();
                if (nearest == 0)
                    nearest = QualityLevels[QualityLevels.Length - 1]; // use lowest
                maxQn = nearest;
            }

            return true;
        }
    }
}
