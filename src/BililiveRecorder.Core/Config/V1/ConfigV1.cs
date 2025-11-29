using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

#nullable disable
namespace BililiveRecorder.Core.Config.V1
{
    [Obsolete]
    [JsonObject(memberSerialization: MemberSerialization.OptIn)]
    internal class ConfigV1 : INotifyPropertyChanged
    {
        //private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 房间号列表
        /// </summary>
        [JsonProperty("roomlist")]
        public List<RoomV1> RoomList { get; set; } = new List<RoomV1>();

        /// <summary>
        /// 启用的功能
        /// </summary>
        //[JsonProperty("feature")]
        //public EnabledFeature EnabledFeature { get => this._enabledFeature; set => this.SetField(ref this._enabledFeature, value); }

        /// <summary>
        /// 剪辑-过去的时长(秒)
        /// </summary>
        [JsonProperty("clip_length_future")]
        public uint ClipLengthFuture { get => this._clipLengthFuture; set => this.SetField(ref this._clipLengthFuture, value); }

        /// <summary>
        /// 剪辑-将来的时长(秒)
        /// </summary>
        [JsonProperty("clip_length_past")]
        public uint ClipLengthPast { get => this._clipLengthPast; set => this.SetField(ref this._clipLengthPast, value); }

        /// <summary>
        /// 自动切割模式
        /// </summary>
        [JsonProperty("cutting_mode")]
        public CuttingMode CuttingMode { get => this._cuttingMode; set => this.SetField(ref this._cuttingMode, value); }

        /// <summary>
        /// 自动切割数值（分钟/MiB）
        /// </summary>
        [JsonProperty("cutting_number")]
        public uint CuttingNumber { get => this._cuttingNumber; set => this.SetField(ref this._cuttingNumber, value); }

        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        [JsonProperty("timing_stream_retry")]
        public uint TimingStreamRetry { get => this._timingStreamRetry; set => this.SetField(ref this._timingStreamRetry, value); }

        /// <summary>
        /// 连接直播服务器超时时间 毫秒
        /// </summary>
        [JsonProperty("timing_stream_connect")]
        public uint TimingStreamConnect { get => this._timingStreamConnect; set => this.SetField(ref this._timingStreamConnect, value); }

        /// <summary>
        /// 弹幕服务器重连时间间隔 毫秒
        /// </summary>
        [JsonProperty("timing_danmaku_retry")]
        public uint TimingDanmakuRetry { get => this._timingDanmakuRetry; set => this.SetField(ref this._timingDanmakuRetry, value); }

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        [JsonProperty("timing_check_interval")]
        public uint TimingCheckInterval { get => this._timingCheckInterval; set => this.SetField(ref this._timingCheckInterval, value); }

        /// <summary>
        /// 最大未收到新直播数据时间 毫秒
        /// </summary>
        [JsonProperty("timing_watchdog_timeout")]
        public uint TimingWatchdogTimeout { get => this._timingWatchdogTimeout; set => this.SetField(ref this._timingWatchdogTimeout, value); }

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        [JsonProperty("cookie")]
        public string Cookie { get => this._cookie; set => this.SetField(ref this._cookie, value); }

        /// <summary>
        /// 是否同时录制弹幕
        /// </summary>
        [JsonProperty("record_danmaku")]
        public bool RecordDanmaku { get => this._recordDanmaku; set => this.SetField(ref this._recordDanmaku, value); }

        /// <summary>
        /// 是否记录弹幕原始数据
        /// </summary>
        [JsonProperty("record_danmaku_raw")]
        public bool RecordDanmakuRaw { get => this._recordDanmakuRaw; set => this.SetField(ref this._recordDanmakuRaw, value); }

        /// <summary>
        /// 是否同时录制 SuperChat
        /// </summary>
        [JsonProperty("record_danmaku_sc")]
        public bool RecordDanmakuSuperChat { get => this._recordDanmakuSuperChat; set => this.SetField(ref this._recordDanmakuSuperChat, value); }

        /// <summary>
        /// 是否同时录制 礼物
        /// </summary>
        [JsonProperty("record_danmaku_gift")]
        public bool RecordDanmakuGift { get => this._recordDanmakuGift; set => this.SetField(ref this._recordDanmakuGift, value); }

        /// <summary>
        /// 是否同时录制 上船
        /// </summary>
        [JsonProperty("record_danmaku_guard")]
        public bool RecordDanmakuGuard { get => this._recordDanmakuGuard; set => this.SetField(ref this._recordDanmakuGuard, value); }

        /// <summary>
        /// 触发 <see cref="System.Xml.XmlWriter.Flush"/> 的弹幕个数
        /// </summary>
        [JsonProperty("record_danmaku_flush_interval")]
        public uint RecordDanmakuFlushInterval { get => this._recordDanmakuFlushInterval; set => this.SetField(ref this._recordDanmakuFlushInterval, value); }

        /// <summary>
        /// 替换api.live.bilibili.com服务器为其他反代，可以支持在云服务器上录制
        /// </summary>
        [JsonProperty("live_api_host")]
        public string LiveApiHost { get => this._liveApiHost; set => this.SetField(ref this._liveApiHost, value); }

        [JsonProperty("record_filename_format")]
        public string RecordFilenameFormat
        {
            get => this._record_filename_format;
            set => this.SetField(ref this._record_filename_format, value);
        }

        [JsonProperty("clip_filename_format")]
        public string ClipFilenameFormat
        {
            get => this._clip_filename_format;
            set => this.SetField(ref this._clip_filename_format, value);
        }

        /// <summary>
        /// Webhook 地址 每行一个
        /// </summary>
        [JsonProperty("webhook_urls")]
        public string WebHookUrls
        {
            get => this._webhook_urls;
            set => this.SetField(ref this._webhook_urls, value);
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            // logger.Trace("设置 [{0}] 的值已从 [{1}] 修改到 [{2}]", propertyName, field, value);
            field = value; this.OnPropertyChanged(propertyName); return true;
        }
        #endregion

        private uint _clipLengthPast = 20;
        private uint _clipLengthFuture = 10;
        private uint _cuttingNumber = 10;
        //private EnabledFeature _enabledFeature = EnabledFeature.RecordOnly;
        private CuttingMode _cuttingMode = CuttingMode.Disabled;

        private uint _timingWatchdogTimeout = 10 * 1000;
        private uint _timingStreamRetry = 6 * 1000;
        private uint _timingStreamConnect = 5 * 1000;
        private uint _timingDanmakuRetry = 15 * 1000;
        private uint _timingCheckInterval = 5 * 60;

        private string _cookie = string.Empty;

        private string _record_filename_format = @"{roomid}-{name}/录制-{roomid}-{date}-{time}-{title}.flv";
        private string _clip_filename_format = @"{roomid}-{name}/剪辑片段-{roomid}-{date}-{time}-{title}.flv";

        private bool _recordDanmaku = false;
        private bool _recordDanmakuRaw = false;
        private bool _recordDanmakuSuperChat = false;
        private bool _recordDanmakuGift = false;
        private bool _recordDanmakuGuard = false;
        private uint _recordDanmakuFlushInterval = 20;

        private string _liveApiHost = "https://api.live.bilibili.com";

        private string _webhook_urls = "";
    }
}
