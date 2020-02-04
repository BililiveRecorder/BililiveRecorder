using BililiveRecorder.FlvProcessor;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BililiveRecorder.Core.Config
{
    [JsonObject(memberSerialization: MemberSerialization.OptIn)]
    public class ConfigV1 : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 当前工作目录
        /// </summary>
        [JsonIgnore]
        [Utils.DoNotCopyProperty]
        public string WorkDirectory { get => _workDirectory; set => SetField(ref _workDirectory, value); }


        /// <summary>
        /// 房间号列表
        /// </summary>
        [JsonProperty("roomlist")]
        public List<RoomV1> RoomList { get; set; } = new List<RoomV1>();

        /// <summary>
        /// 启用的功能
        /// </summary>
        [JsonProperty("feature")]
        public EnabledFeature EnabledFeature { get => _enabledFeature; set => SetField(ref _enabledFeature, value); }

        /// <summary>
        /// 剪辑-过去的时长(秒)
        /// </summary>
        [JsonProperty("clip_length_future")]
        public uint ClipLengthFuture { get => _clipLengthFuture; set => SetField(ref _clipLengthFuture, value); }

        /// <summary>
        /// 剪辑-将来的时长(秒)
        /// </summary>
        [JsonProperty("clip_length_past")]
        public uint ClipLengthPast { get => _clipLengthPast; set => SetField(ref _clipLengthPast, value); }

        /// <summary>
        /// 自动切割模式
        /// </summary>
        [JsonProperty("cutting_mode")]
        public AutoCuttingMode CuttingMode { get => _cuttingMode; set => SetField(ref _cuttingMode, value); }

        /// <summary>
        /// 自动切割数值（分钟/MiB）
        /// </summary>
        [JsonProperty("cutting_number")]
        public uint CuttingNumber { get => _cuttingNumber; set => SetField(ref _cuttingNumber, value); }

        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        [JsonProperty("timing_stream_retry")]
        public uint TimingStreamRetry { get => _timingStreamRetry; set => SetField(ref _timingStreamRetry, value); }

        /// <summary>
        /// 连接直播服务器超时时间 毫秒
        /// </summary>
        [JsonProperty("timing_stream_connect")]
        public uint TimingStreamConnect { get => _timingStreamConnect; set => SetField(ref _timingStreamConnect, value); }

        /// <summary>
        /// 弹幕服务器重连时间间隔 毫秒
        /// </summary>
        [JsonProperty("timing_danmaku_retry")]
        public uint TimingDanmakuRetry { get => _timingDanmakuRetry; set => SetField(ref _timingDanmakuRetry, value); }

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        [JsonProperty("timing_check_interval")]
        public uint TimingCheckInterval { get => _timingCheckInterval; set => SetField(ref _timingCheckInterval, value); }

        /// <summary>
        /// 最大未收到新直播数据时间 毫秒
        /// </summary>
        [JsonProperty("timing_watchdog_timeout")]
        public uint TimingWatchdogTimeout { get => _timingWatchdogTimeout; set => SetField(ref _timingWatchdogTimeout, value); }

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        [JsonProperty("cookie")]
        public string Cookie { get => _cookie; set => SetField(ref _cookie, value); }

        /// <summary>
        /// 尽量避开腾讯云服务器，可有效提升录制文件能正常播放的概率。（垃圾腾讯云直播服务）
        /// </summary>
        [JsonProperty("avoidtxy")]
        public bool AvoidTxy { get => _avoidTxy; set => SetField(ref _avoidTxy, value); }

        /// <summary>
        /// 是否记录文本弹幕
        /// </summary>
        [JsonProperty("RecDanmaku")]
        public bool RecDanmaku { get => _rec_danmaku; set => SetField(ref _rec_danmaku, value); }

        /// <summary>
        /// 是否记录礼物
        /// </summary>
        [JsonProperty("RecDanmaku_gift")]
        public bool RecDanmaku_gift { get => _rec_danmaku_gift; set => SetField(ref _rec_danmaku_gift, value); }

        /// <summary>
        /// 是否记录欢迎老爷
        /// </summary>
        [JsonProperty("RecDanmaku_welcome")]
        public bool RecDanmaku_welcome { get => _rec_danmaku_welcome; set => SetField(ref _rec_danmaku_welcome, value); }

        /// <summary>
        /// 是否记录欢迎船员
        /// </summary>
        [JsonProperty("RecDanmaku_welguard")]
        public bool RecDanmaku_welguard { get => _rec_danmaku_welguard; set => SetField(ref _rec_danmaku_welguard, value); }

        /// <summary>
        /// 是否记录上船
        /// </summary>
        [JsonProperty("RecDanmaku_guardbuy")]
        public bool RecDanmaku_guardbuy { get => _rec_danmaku_guardbuy; set => SetField(ref _rec_danmaku_guardbuy, value); }

        /// <summary>
        /// 是否记录未知弹幕类型
        /// </summary>
        [JsonProperty("RecDanmaku_unknown")]
        public bool RecDanmaku_unknown { get => _rec_danmaku_unknown; set => SetField(ref _rec_danmaku_unknown, value); }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) { return false; }
            logger.Debug("设置 [{0}] 的值已从 [{1}] 修改到 [{2}]", propertyName, field, value);
            field = value; OnPropertyChanged(propertyName); return true;
        }
        #endregion

        private uint _clipLengthPast = 20;
        private uint _clipLengthFuture = 10;
        private uint _cuttingNumber = 10;
        private EnabledFeature _enabledFeature = EnabledFeature.Both;
        private AutoCuttingMode _cuttingMode = AutoCuttingMode.Disabled;
        private string _workDirectory;

        private uint _timingWatchdogTimeout = 10 * 1000;
        private uint _timingStreamRetry = 6 * 1000;
        private uint _timingStreamConnect = 3 * 1000;
        private uint _timingDanmakuRetry = 2 * 1000;
        private uint _timingCheckInterval = 5 * 60;

        private bool _rec_danmaku = false;
        private bool _rec_danmaku_gift = false;
        private bool _rec_danmaku_welcome = false;
        private bool _rec_danmaku_welguard = false;
        private bool _rec_danmaku_guardbuy = false;
        private bool _rec_danmaku_unknown = false;

        private string _cookie = string.Empty;

        private bool _avoidTxy = true;
    }
}
