using System;

namespace BililiveRecorder.Core.Event
{
    public sealed class RecordingStatsEventArgs : EventArgs
    {
        /// <summary>
        /// 从录制开始到现在一共经过的时间，毫秒
        /// </summary>
        public double SessionDuration { get; set; }

        /// <summary>
        /// 总接受字节数
        /// </summary>
        public long TotalInputBytes { get; set; }

        /// <summary>
        /// 总写入字节数
        /// </summary>
        public long TotalOutputBytes { get; set; }

        /// <summary>
        /// 当前文件的大小
        /// </summary>
        public long CurrentFileSize { get; set; }

        /// <summary>
        /// 本次直播流收到的最大时间戳（已修复过，相当于总时长，毫秒）
        /// </summary>
        public int SessionMaxTimestamp { get; set; }

        /// <summary>
        /// 当前文件的最大时间戳（相当于总时长，毫秒）
        /// </summary>
        public int FileMaxTimestamp { get; set; }

        /// <summary>
        /// 当前这一个统计区间的直播数据时长，毫秒
        /// </summary>
        public double AddedDuration { get; set; }

        /// <summary>
        /// 当前这一个统计区间所经过的时间长度，毫秒
        /// </summary>
        public double PassedTime { get; set; }

        /// <summary>
        /// 录制速度比例
        /// </summary>
        public double DurationRatio { get; set; }

        //----------------------------------------

        /// <summary>
        /// 当前统计区间新收到的视频数据大小
        /// </summary>
        public long InputVideoBytes { get; set; }
        /// <summary>
        /// 当前统计区间新收到的音频数据大小
        /// </summary>
        public long InputAudioBytes { get; set; }

        /// <summary>
        /// 当前统计区间新写入的视频帧数量
        /// </summary>
        public int OutputVideoFrames { get; set; }
        /// <summary>
        /// 当前统计区间新写入的音频帧数量
        /// </summary>
        public int OutputAudioFrames { get; set; }
        /// <summary>
        /// 当前统计区间新写入的视频数据大小
        /// </summary>
        public long OutputVideoBytes { get; set; }
        /// <summary>
        /// 当前统计区间新写入的音频数据大小
        /// </summary>
        public long OutputAudioBytes { get; set; }

        /// <summary>
        /// 总共收到的视频数据大小
        /// </summary>
        public long TotalInputVideoBytes { get; set; }
        /// <summary>
        /// 总共收到的音频数据大小
        /// </summary>
        public long TotalInputAudioBytes { get; set; }

        /// <summary>
        /// 总共写入的视频帧数量
        /// </summary>
        public int TotalOutputVideoFrames { get; set; }
        /// <summary>
        /// 总共写入的音频帧数量
        /// </summary>
        public int TotalOutputAudioFrames { get; set; }
        /// <summary>
        /// 总共写入的视频数据大小
        /// </summary>
        public long TotalOutputVideoBytes { get; set; }
        /// <summary>
        /// 总共写入的音频数据大小
        /// </summary>
        public long TotalOutputAudioBytes { get; set; }
    }
}
