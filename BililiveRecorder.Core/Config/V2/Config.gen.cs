// ******************************
//  GENERATED CODE, DO NOT EDIT.
//  RUN FORMATTER AFTER GENERATE
// ******************************
using System.ComponentModel;
using HierarchicalPropertyDefault;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V2
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed partial class RoomConfig : HierarchicalObject<GlobalConfig, RoomConfig>
    {
        /// <summary>
        /// 房间号
        /// </summary>
        public int RoomId { get => this.GetPropertyValue<int>(); set => this.SetPropertyValue(value); }
        public bool HasRoomId { get => this.GetPropertyHasValue(nameof(this.RoomId)); set => this.SetPropertyHasValue<int>(value, nameof(this.RoomId)); }
        [JsonProperty(nameof(RoomId)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<int> OptionalRoomId { get => this.GetPropertyValueOptional<int>(nameof(this.RoomId)); set => this.SetPropertyValueOptional(value, nameof(this.RoomId)); }

        /// <summary>
        /// 是否启用自动录制
        /// </summary>
        public bool AutoRecord { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasAutoRecord { get => this.GetPropertyHasValue(nameof(this.AutoRecord)); set => this.SetPropertyHasValue<bool>(value, nameof(this.AutoRecord)); }
        [JsonProperty(nameof(AutoRecord)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalAutoRecord { get => this.GetPropertyValueOptional<bool>(nameof(this.AutoRecord)); set => this.SetPropertyValueOptional(value, nameof(this.AutoRecord)); }

        /// <summary>
        /// 录制文件自动切割模式
        /// </summary>
        public CuttingMode CuttingMode { get => this.GetPropertyValue<CuttingMode>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingMode { get => this.GetPropertyHasValue(nameof(this.CuttingMode)); set => this.SetPropertyHasValue<CuttingMode>(value, nameof(this.CuttingMode)); }
        [JsonProperty(nameof(CuttingMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<CuttingMode> OptionalCuttingMode { get => this.GetPropertyValueOptional<CuttingMode>(nameof(this.CuttingMode)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingMode)); }

        /// <summary>
        /// 录制文件自动切割数值（分钟/MiB）
        /// </summary>
        public uint CuttingNumber { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingNumber { get => this.GetPropertyHasValue(nameof(this.CuttingNumber)); set => this.SetPropertyHasValue<uint>(value, nameof(this.CuttingNumber)); }
        [JsonProperty(nameof(CuttingNumber)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalCuttingNumber { get => this.GetPropertyValueOptional<uint>(nameof(this.CuttingNumber)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingNumber)); }

        /// <summary>
        /// 是否同时录制弹幕
        /// </summary>
        public bool RecordDanmaku { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmaku { get => this.GetPropertyHasValue(nameof(this.RecordDanmaku)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmaku)); }
        [JsonProperty(nameof(RecordDanmaku)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmaku { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmaku)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmaku)); }

        /// <summary>
        /// 是否记录弹幕原始数据
        /// </summary>
        public bool RecordDanmakuRaw { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuRaw { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuRaw)); }
        [JsonProperty(nameof(RecordDanmakuRaw)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuRaw { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuRaw)); }

        /// <summary>
        /// 是否同时录制 SuperChat
        /// </summary>
        public bool RecordDanmakuSuperChat { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuSuperChat { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuSuperChat)); }
        [JsonProperty(nameof(RecordDanmakuSuperChat)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuSuperChat { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuSuperChat)); }

        /// <summary>
        /// 是否同时录制 礼物
        /// </summary>
        public bool RecordDanmakuGift { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGift { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGift)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGift)); }
        [JsonProperty(nameof(RecordDanmakuGift)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGift { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGift)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGift)); }

        /// <summary>
        /// 是否同时录制 上船
        /// </summary>
        public bool RecordDanmakuGuard { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGuard { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGuard)); }
        [JsonProperty(nameof(RecordDanmakuGuard)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGuard { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGuard)); }

        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        public uint TimingStreamRetry => this.GetPropertyValue<uint>();

        /// <summary>
        /// 连接直播服务器超时时间 毫秒
        /// </summary>
        public uint TimingStreamConnect => this.GetPropertyValue<uint>();

        /// <summary>
        /// 弹幕服务器重连时间间隔 毫秒
        /// </summary>
        public uint TimingDanmakuRetry => this.GetPropertyValue<uint>();

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 最大未收到新直播数据时间 毫秒
        /// </summary>
        public uint TimingWatchdogTimeout => this.GetPropertyValue<uint>();

        /// <summary>
        /// 触发 <see cref="System.Xml.XmlWriter.Flush"/> 的弹幕个数
        /// </summary>
        public uint RecordDanmakuFlushInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        public string? Cookie => this.GetPropertyValue<string>();

        /// <summary>
        /// 录制文件写入结束 Webhook 地址 每行一个
        /// </summary>
        public string? WebHookUrls => this.GetPropertyValue<string>();

        /// <summary>
        /// Webhook v2 地址 每行一个
        /// </summary>
        public string? WebHookUrlsV2 => this.GetPropertyValue<string>();

        /// <summary>
        /// 替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制
        /// </summary>
        public string? LiveApiHost => this.GetPropertyValue<string>();

        /// <summary>
        /// 录制文件名模板
        /// </summary>
        public string? RecordFilenameFormat => this.GetPropertyValue<string>();

        /// <summary>
        /// 是否显示直播间标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea => this.GetPropertyValue<bool>();

    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed partial class GlobalConfig : HierarchicalObject<DefaultConfig, GlobalConfig>
    {
        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        public uint TimingStreamRetry { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingStreamRetry { get => this.GetPropertyHasValue(nameof(this.TimingStreamRetry)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingStreamRetry)); }
        [JsonProperty(nameof(TimingStreamRetry)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingStreamRetry { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingStreamRetry)); set => this.SetPropertyValueOptional(value, nameof(this.TimingStreamRetry)); }

        /// <summary>
        /// 连接直播服务器超时时间 毫秒
        /// </summary>
        public uint TimingStreamConnect { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingStreamConnect { get => this.GetPropertyHasValue(nameof(this.TimingStreamConnect)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingStreamConnect)); }
        [JsonProperty(nameof(TimingStreamConnect)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingStreamConnect { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingStreamConnect)); set => this.SetPropertyValueOptional(value, nameof(this.TimingStreamConnect)); }

        /// <summary>
        /// 弹幕服务器重连时间间隔 毫秒
        /// </summary>
        public uint TimingDanmakuRetry { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingDanmakuRetry { get => this.GetPropertyHasValue(nameof(this.TimingDanmakuRetry)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingDanmakuRetry)); }
        [JsonProperty(nameof(TimingDanmakuRetry)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingDanmakuRetry { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingDanmakuRetry)); set => this.SetPropertyValueOptional(value, nameof(this.TimingDanmakuRetry)); }

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingCheckInterval { get => this.GetPropertyHasValue(nameof(this.TimingCheckInterval)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingCheckInterval)); }
        [JsonProperty(nameof(TimingCheckInterval)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingCheckInterval { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingCheckInterval)); set => this.SetPropertyValueOptional(value, nameof(this.TimingCheckInterval)); }

        /// <summary>
        /// 最大未收到新直播数据时间 毫秒
        /// </summary>
        public uint TimingWatchdogTimeout { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingWatchdogTimeout { get => this.GetPropertyHasValue(nameof(this.TimingWatchdogTimeout)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingWatchdogTimeout)); }
        [JsonProperty(nameof(TimingWatchdogTimeout)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingWatchdogTimeout { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingWatchdogTimeout)); set => this.SetPropertyValueOptional(value, nameof(this.TimingWatchdogTimeout)); }

        /// <summary>
        /// 触发 <see cref="System.Xml.XmlWriter.Flush"/> 的弹幕个数
        /// </summary>
        public uint RecordDanmakuFlushInterval { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuFlushInterval { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuFlushInterval)); set => this.SetPropertyHasValue<uint>(value, nameof(this.RecordDanmakuFlushInterval)); }
        [JsonProperty(nameof(RecordDanmakuFlushInterval)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalRecordDanmakuFlushInterval { get => this.GetPropertyValueOptional<uint>(nameof(this.RecordDanmakuFlushInterval)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuFlushInterval)); }

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        public string? Cookie { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasCookie { get => this.GetPropertyHasValue(nameof(this.Cookie)); set => this.SetPropertyHasValue<string>(value, nameof(this.Cookie)); }
        [JsonProperty(nameof(Cookie)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalCookie { get => this.GetPropertyValueOptional<string>(nameof(this.Cookie)); set => this.SetPropertyValueOptional(value, nameof(this.Cookie)); }

        /// <summary>
        /// 录制文件写入结束 Webhook 地址 每行一个
        /// </summary>
        public string? WebHookUrls { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasWebHookUrls { get => this.GetPropertyHasValue(nameof(this.WebHookUrls)); set => this.SetPropertyHasValue<string>(value, nameof(this.WebHookUrls)); }
        [JsonProperty(nameof(WebHookUrls)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalWebHookUrls { get => this.GetPropertyValueOptional<string>(nameof(this.WebHookUrls)); set => this.SetPropertyValueOptional(value, nameof(this.WebHookUrls)); }

        /// <summary>
        /// Webhook v2 地址 每行一个
        /// </summary>
        public string? WebHookUrlsV2 { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasWebHookUrlsV2 { get => this.GetPropertyHasValue(nameof(this.WebHookUrlsV2)); set => this.SetPropertyHasValue<string>(value, nameof(this.WebHookUrlsV2)); }
        [JsonProperty(nameof(WebHookUrlsV2)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalWebHookUrlsV2 { get => this.GetPropertyValueOptional<string>(nameof(this.WebHookUrlsV2)); set => this.SetPropertyValueOptional(value, nameof(this.WebHookUrlsV2)); }

        /// <summary>
        /// 替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制
        /// </summary>
        public string? LiveApiHost { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasLiveApiHost { get => this.GetPropertyHasValue(nameof(this.LiveApiHost)); set => this.SetPropertyHasValue<string>(value, nameof(this.LiveApiHost)); }
        [JsonProperty(nameof(LiveApiHost)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalLiveApiHost { get => this.GetPropertyValueOptional<string>(nameof(this.LiveApiHost)); set => this.SetPropertyValueOptional(value, nameof(this.LiveApiHost)); }

        /// <summary>
        /// 录制文件名模板
        /// </summary>
        public string? RecordFilenameFormat { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasRecordFilenameFormat { get => this.GetPropertyHasValue(nameof(this.RecordFilenameFormat)); set => this.SetPropertyHasValue<string>(value, nameof(this.RecordFilenameFormat)); }
        [JsonProperty(nameof(RecordFilenameFormat)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalRecordFilenameFormat { get => this.GetPropertyValueOptional<string>(nameof(this.RecordFilenameFormat)); set => this.SetPropertyValueOptional(value, nameof(this.RecordFilenameFormat)); }

        /// <summary>
        /// 是否显示直播间标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasWpfShowTitleAndArea { get => this.GetPropertyHasValue(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyHasValue<bool>(value, nameof(this.WpfShowTitleAndArea)); }
        [JsonProperty(nameof(WpfShowTitleAndArea)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalWpfShowTitleAndArea { get => this.GetPropertyValueOptional<bool>(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyValueOptional(value, nameof(this.WpfShowTitleAndArea)); }

        /// <summary>
        /// 录制文件自动切割模式
        /// </summary>
        public CuttingMode CuttingMode { get => this.GetPropertyValue<CuttingMode>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingMode { get => this.GetPropertyHasValue(nameof(this.CuttingMode)); set => this.SetPropertyHasValue<CuttingMode>(value, nameof(this.CuttingMode)); }
        [JsonProperty(nameof(CuttingMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<CuttingMode> OptionalCuttingMode { get => this.GetPropertyValueOptional<CuttingMode>(nameof(this.CuttingMode)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingMode)); }

        /// <summary>
        /// 录制文件自动切割数值（分钟/MiB）
        /// </summary>
        public uint CuttingNumber { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingNumber { get => this.GetPropertyHasValue(nameof(this.CuttingNumber)); set => this.SetPropertyHasValue<uint>(value, nameof(this.CuttingNumber)); }
        [JsonProperty(nameof(CuttingNumber)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalCuttingNumber { get => this.GetPropertyValueOptional<uint>(nameof(this.CuttingNumber)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingNumber)); }

        /// <summary>
        /// 是否同时录制弹幕
        /// </summary>
        public bool RecordDanmaku { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmaku { get => this.GetPropertyHasValue(nameof(this.RecordDanmaku)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmaku)); }
        [JsonProperty(nameof(RecordDanmaku)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmaku { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmaku)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmaku)); }

        /// <summary>
        /// 是否记录弹幕原始数据
        /// </summary>
        public bool RecordDanmakuRaw { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuRaw { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuRaw)); }
        [JsonProperty(nameof(RecordDanmakuRaw)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuRaw { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuRaw)); }

        /// <summary>
        /// 是否同时录制 SuperChat
        /// </summary>
        public bool RecordDanmakuSuperChat { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuSuperChat { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuSuperChat)); }
        [JsonProperty(nameof(RecordDanmakuSuperChat)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuSuperChat { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuSuperChat)); }

        /// <summary>
        /// 是否同时录制 礼物
        /// </summary>
        public bool RecordDanmakuGift { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGift { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGift)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGift)); }
        [JsonProperty(nameof(RecordDanmakuGift)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGift { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGift)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGift)); }

        /// <summary>
        /// 是否同时录制 上船
        /// </summary>
        public bool RecordDanmakuGuard { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGuard { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGuard)); }
        [JsonProperty(nameof(RecordDanmakuGuard)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGuard { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGuard)); }

    }

    public sealed partial class DefaultConfig
    {
        internal static readonly DefaultConfig Instance = new DefaultConfig();
        private DefaultConfig() { }

        public uint TimingStreamRetry => 6 * 1000;

        public uint TimingStreamConnect => 5 * 1000;

        public uint TimingDanmakuRetry => 15 * 1000;

        public uint TimingCheckInterval => 10 * 60;

        public uint TimingWatchdogTimeout => 10 * 1000;

        public uint RecordDanmakuFlushInterval => 20;

        public string Cookie => string.Empty;

        public string WebHookUrls => string.Empty;

        public string WebHookUrlsV2 => string.Empty;

        public string LiveApiHost => "https://api.live.bilibili.com";

        public string RecordFilenameFormat => @"{roomid}-{name}/录制-{roomid}-{date}-{time}-{title}.flv";

        public bool WpfShowTitleAndArea => true;

        public CuttingMode CuttingMode => CuttingMode.Disabled;

        public uint CuttingNumber => 100;

        public bool RecordDanmaku => false;

        public bool RecordDanmakuRaw => false;

        public bool RecordDanmakuSuperChat => true;

        public bool RecordDanmakuGift => false;

        public bool RecordDanmakuGuard => true;

    }

}
