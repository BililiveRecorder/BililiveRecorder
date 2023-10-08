using System;
namespace BililiveRecorder.Core.Config
{
    [Flags]
    public enum CuttingMode : int
    {
        /// <summary>
        /// 禁用
        /// </summary>
        Disabled = 0, // 0
        /// <summary>
        /// 根据时间切割
        /// </summary>
        ByTime = 1 << 0, // 1
        /// <summary>
        /// 根据文件大小切割
        /// </summary>
        BySize = 1 << 1, // 2, 0b_0010
        /// <summary>
        /// 根据直播间标题切割
        /// </summary>
        ByTitle = 1 << 2, // 4, 0b_0100
    }
}
