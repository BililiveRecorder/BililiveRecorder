using System.Runtime.CompilerServices;

namespace BililiveRecorder.Core.Templating
{
    internal static class StreamQualityNumber
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string MapToString(int qn) => qn switch
        {
            30000 => "杜比",
            20000 => "4K",
            10000 => "原画",
            401 => "蓝光(杜比)",
            400 => "蓝光",
            250 => "超清",
            150 => "高清",
            80 => "流畅",
            -1 => "录播姬脚本",
            _ => $"未知({qn})"
        };
    }
}
