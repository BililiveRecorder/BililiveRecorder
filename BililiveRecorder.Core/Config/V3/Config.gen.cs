// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE /config_gen/README.md
// ******************************

using System.ComponentModel;
using HierarchicalPropertyDefault;
using Newtonsoft.Json;

#nullable enable
namespace BililiveRecorder.Core.Config.V3
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
        /// 自动录制
        /// </summary>
        public bool AutoRecord { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasAutoRecord { get => this.GetPropertyHasValue(nameof(this.AutoRecord)); set => this.SetPropertyHasValue<bool>(value, nameof(this.AutoRecord)); }
        [JsonProperty(nameof(AutoRecord)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalAutoRecord { get => this.GetPropertyValueOptional<bool>(nameof(this.AutoRecord)); set => this.SetPropertyValueOptional(value, nameof(this.AutoRecord)); }

        /// <summary>
        /// 录制模式
        /// </summary>
        public RecordMode RecordMode { get => this.GetPropertyValue<RecordMode>(); set => this.SetPropertyValue(value); }
        public bool HasRecordMode { get => this.GetPropertyHasValue(nameof(this.RecordMode)); set => this.SetPropertyHasValue<RecordMode>(value, nameof(this.RecordMode)); }
        [JsonProperty(nameof(RecordMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<RecordMode> OptionalRecordMode { get => this.GetPropertyValueOptional<RecordMode>(nameof(this.RecordMode)); set => this.SetPropertyValueOptional(value, nameof(this.RecordMode)); }

        /// <summary>
        /// 自动分段模式
        /// </summary>
        public CuttingMode CuttingMode { get => this.GetPropertyValue<CuttingMode>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingMode { get => this.GetPropertyHasValue(nameof(this.CuttingMode)); set => this.SetPropertyHasValue<CuttingMode>(value, nameof(this.CuttingMode)); }
        [JsonProperty(nameof(CuttingMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<CuttingMode> OptionalCuttingMode { get => this.GetPropertyValueOptional<CuttingMode>(nameof(this.CuttingMode)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingMode)); }

        /// <summary>
        /// 自动分段数值
        /// </summary>
        public uint CuttingNumber { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingNumber { get => this.GetPropertyHasValue(nameof(this.CuttingNumber)); set => this.SetPropertyHasValue<uint>(value, nameof(this.CuttingNumber)); }
        [JsonProperty(nameof(CuttingNumber)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalCuttingNumber { get => this.GetPropertyValueOptional<uint>(nameof(this.CuttingNumber)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingNumber)); }

        /// <summary>
        /// 弹幕录制
        /// </summary>
        public bool RecordDanmaku { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmaku { get => this.GetPropertyHasValue(nameof(this.RecordDanmaku)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmaku)); }
        [JsonProperty(nameof(RecordDanmaku)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmaku { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmaku)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmaku)); }

        /// <summary>
        /// 弹幕录制-原始数据
        /// </summary>
        public bool RecordDanmakuRaw { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuRaw { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuRaw)); }
        [JsonProperty(nameof(RecordDanmakuRaw)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuRaw { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuRaw)); }

        /// <summary>
        /// 弹幕录制-SuperChat
        /// </summary>
        public bool RecordDanmakuSuperChat { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuSuperChat { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuSuperChat)); }
        [JsonProperty(nameof(RecordDanmakuSuperChat)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuSuperChat { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuSuperChat)); }

        /// <summary>
        /// 弹幕录制-礼物
        /// </summary>
        public bool RecordDanmakuGift { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGift { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGift)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGift)); }
        [JsonProperty(nameof(RecordDanmakuGift)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGift { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGift)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGift)); }

        /// <summary>
        /// 弹幕录制-上船
        /// </summary>
        public bool RecordDanmakuGuard { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGuard { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGuard)); }
        [JsonProperty(nameof(RecordDanmakuGuard)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGuard { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGuard)); }

        /// <summary>
        /// 直播画质
        /// </summary>
        public string? RecordingQuality { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasRecordingQuality { get => this.GetPropertyHasValue(nameof(this.RecordingQuality)); set => this.SetPropertyHasValue<string>(value, nameof(this.RecordingQuality)); }
        [JsonProperty(nameof(RecordingQuality)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalRecordingQuality { get => this.GetPropertyValueOptional<string>(nameof(this.RecordingQuality)); set => this.SetPropertyValueOptional(value, nameof(this.RecordingQuality)); }

        /// <summary>
        /// 录制文件名模板
        /// </summary>
        public string? FileNameRecordTemplate => this.GetPropertyValue<string>();

        /// <summary>
        /// 录制文件写入结束 Webhook 地址 每行一个
        /// </summary>
        public string? WebHookUrls => this.GetPropertyValue<string>();

        /// <summary>
        /// Webhook v2 地址 每行一个
        /// </summary>
        public string? WebHookUrlsV2 => this.GetPropertyValue<string>();

        /// <summary>
        /// 在界面显示标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea => this.GetPropertyValue<bool>();

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        public string? Cookie => this.GetPropertyValue<string>();

        /// <summary>
        /// 替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制
        /// </summary>
        public string? LiveApiHost => this.GetPropertyValue<string>();

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        public uint TimingStreamRetry => this.GetPropertyValue<uint>();

        /// <summary>
        /// 录制无指定画质重连时间间隔 秒
        /// </summary>
        public uint TimingStreamRetryNoQn => this.GetPropertyValue<uint>();

        /// <summary>
        /// 连接直播服务器超时时间 毫秒
        /// </summary>
        public uint TimingStreamConnect => this.GetPropertyValue<uint>();

        /// <summary>
        /// 弹幕服务器重连时间间隔 毫秒
        /// </summary>
        public uint TimingDanmakuRetry => this.GetPropertyValue<uint>();

        /// <summary>
        /// 最大允许未收到直播数据时间 毫秒
        /// </summary>
        public uint TimingWatchdogTimeout => this.GetPropertyValue<uint>();

        /// <summary>
        /// 触发 <see cref="System.Xml.XmlWriter.Flush"/> 的弹幕个数
        /// </summary>
        public uint RecordDanmakuFlushInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 是否使用系统代理
        /// </summary>
        public bool NetworkTransportUseSystemProxy => this.GetPropertyValue<bool>();

        /// <summary>
        /// 允许使用的 IP 网络类型
        /// </summary>
        public AllowedAddressFamily NetworkTransportAllowedAddressFamily => this.GetPropertyValue<AllowedAddressFamily>();

        /// <summary>
        /// 自定义脚本
        /// </summary>
        public string? UserScript => this.GetPropertyValue<string>();

    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed partial class GlobalConfig : HierarchicalObject<DefaultConfig, GlobalConfig>
    {
        /// <summary>
        /// 录制模式
        /// </summary>
        public RecordMode RecordMode { get => this.GetPropertyValue<RecordMode>(); set => this.SetPropertyValue(value); }
        public bool HasRecordMode { get => this.GetPropertyHasValue(nameof(this.RecordMode)); set => this.SetPropertyHasValue<RecordMode>(value, nameof(this.RecordMode)); }
        [JsonProperty(nameof(RecordMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<RecordMode> OptionalRecordMode { get => this.GetPropertyValueOptional<RecordMode>(nameof(this.RecordMode)); set => this.SetPropertyValueOptional(value, nameof(this.RecordMode)); }

        /// <summary>
        /// 自动分段模式
        /// </summary>
        public CuttingMode CuttingMode { get => this.GetPropertyValue<CuttingMode>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingMode { get => this.GetPropertyHasValue(nameof(this.CuttingMode)); set => this.SetPropertyHasValue<CuttingMode>(value, nameof(this.CuttingMode)); }
        [JsonProperty(nameof(CuttingMode)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<CuttingMode> OptionalCuttingMode { get => this.GetPropertyValueOptional<CuttingMode>(nameof(this.CuttingMode)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingMode)); }

        /// <summary>
        /// 自动分段数值
        /// </summary>
        public uint CuttingNumber { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingNumber { get => this.GetPropertyHasValue(nameof(this.CuttingNumber)); set => this.SetPropertyHasValue<uint>(value, nameof(this.CuttingNumber)); }
        [JsonProperty(nameof(CuttingNumber)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalCuttingNumber { get => this.GetPropertyValueOptional<uint>(nameof(this.CuttingNumber)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingNumber)); }

        /// <summary>
        /// 弹幕录制
        /// </summary>
        public bool RecordDanmaku { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmaku { get => this.GetPropertyHasValue(nameof(this.RecordDanmaku)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmaku)); }
        [JsonProperty(nameof(RecordDanmaku)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmaku { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmaku)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmaku)); }

        /// <summary>
        /// 弹幕录制-原始数据
        /// </summary>
        public bool RecordDanmakuRaw { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuRaw { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuRaw)); }
        [JsonProperty(nameof(RecordDanmakuRaw)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuRaw { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuRaw)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuRaw)); }

        /// <summary>
        /// 弹幕录制-SuperChat
        /// </summary>
        public bool RecordDanmakuSuperChat { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuSuperChat { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuSuperChat)); }
        [JsonProperty(nameof(RecordDanmakuSuperChat)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuSuperChat { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuSuperChat)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuSuperChat)); }

        /// <summary>
        /// 弹幕录制-礼物
        /// </summary>
        public bool RecordDanmakuGift { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGift { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGift)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGift)); }
        [JsonProperty(nameof(RecordDanmakuGift)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGift { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGift)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGift)); }

        /// <summary>
        /// 弹幕录制-上船
        /// </summary>
        public bool RecordDanmakuGuard { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuGuard { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyHasValue<bool>(value, nameof(this.RecordDanmakuGuard)); }
        [JsonProperty(nameof(RecordDanmakuGuard)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalRecordDanmakuGuard { get => this.GetPropertyValueOptional<bool>(nameof(this.RecordDanmakuGuard)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuGuard)); }

        /// <summary>
        /// 直播画质
        /// </summary>
        public string? RecordingQuality { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasRecordingQuality { get => this.GetPropertyHasValue(nameof(this.RecordingQuality)); set => this.SetPropertyHasValue<string>(value, nameof(this.RecordingQuality)); }
        [JsonProperty(nameof(RecordingQuality)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalRecordingQuality { get => this.GetPropertyValueOptional<string>(nameof(this.RecordingQuality)); set => this.SetPropertyValueOptional(value, nameof(this.RecordingQuality)); }

        /// <summary>
        /// 录制文件名模板
        /// </summary>
        public string? FileNameRecordTemplate { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasFileNameRecordTemplate { get => this.GetPropertyHasValue(nameof(this.FileNameRecordTemplate)); set => this.SetPropertyHasValue<string>(value, nameof(this.FileNameRecordTemplate)); }
        [JsonProperty(nameof(FileNameRecordTemplate)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalFileNameRecordTemplate { get => this.GetPropertyValueOptional<string>(nameof(this.FileNameRecordTemplate)); set => this.SetPropertyValueOptional(value, nameof(this.FileNameRecordTemplate)); }

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
        /// 在界面显示标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasWpfShowTitleAndArea { get => this.GetPropertyHasValue(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyHasValue<bool>(value, nameof(this.WpfShowTitleAndArea)); }
        [JsonProperty(nameof(WpfShowTitleAndArea)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalWpfShowTitleAndArea { get => this.GetPropertyValueOptional<bool>(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyValueOptional(value, nameof(this.WpfShowTitleAndArea)); }

        /// <summary>
        /// 请求 API 时使用的 Cookie
        /// </summary>
        public string? Cookie { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasCookie { get => this.GetPropertyHasValue(nameof(this.Cookie)); set => this.SetPropertyHasValue<string>(value, nameof(this.Cookie)); }
        [JsonProperty(nameof(Cookie)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalCookie { get => this.GetPropertyValueOptional<string>(nameof(this.Cookie)); set => this.SetPropertyValueOptional(value, nameof(this.Cookie)); }

        /// <summary>
        /// 替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制
        /// </summary>
        public string? LiveApiHost { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasLiveApiHost { get => this.GetPropertyHasValue(nameof(this.LiveApiHost)); set => this.SetPropertyHasValue<string>(value, nameof(this.LiveApiHost)); }
        [JsonProperty(nameof(LiveApiHost)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalLiveApiHost { get => this.GetPropertyValueOptional<string>(nameof(this.LiveApiHost)); set => this.SetPropertyValueOptional(value, nameof(this.LiveApiHost)); }

        /// <summary>
        /// HTTP API 检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingCheckInterval { get => this.GetPropertyHasValue(nameof(this.TimingCheckInterval)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingCheckInterval)); }
        [JsonProperty(nameof(TimingCheckInterval)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingCheckInterval { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingCheckInterval)); set => this.SetPropertyValueOptional(value, nameof(this.TimingCheckInterval)); }

        /// <summary>
        /// 录制断开重连时间间隔 毫秒
        /// </summary>
        public uint TimingStreamRetry { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingStreamRetry { get => this.GetPropertyHasValue(nameof(this.TimingStreamRetry)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingStreamRetry)); }
        [JsonProperty(nameof(TimingStreamRetry)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingStreamRetry { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingStreamRetry)); set => this.SetPropertyValueOptional(value, nameof(this.TimingStreamRetry)); }

        /// <summary>
        /// 录制无指定画质重连时间间隔 秒
        /// </summary>
        public uint TimingStreamRetryNoQn { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingStreamRetryNoQn { get => this.GetPropertyHasValue(nameof(this.TimingStreamRetryNoQn)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingStreamRetryNoQn)); }
        [JsonProperty(nameof(TimingStreamRetryNoQn)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingStreamRetryNoQn { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingStreamRetryNoQn)); set => this.SetPropertyValueOptional(value, nameof(this.TimingStreamRetryNoQn)); }

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
        /// 最大允许未收到直播数据时间 毫秒
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
        /// 是否使用系统代理
        /// </summary>
        public bool NetworkTransportUseSystemProxy { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasNetworkTransportUseSystemProxy { get => this.GetPropertyHasValue(nameof(this.NetworkTransportUseSystemProxy)); set => this.SetPropertyHasValue<bool>(value, nameof(this.NetworkTransportUseSystemProxy)); }
        [JsonProperty(nameof(NetworkTransportUseSystemProxy)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalNetworkTransportUseSystemProxy { get => this.GetPropertyValueOptional<bool>(nameof(this.NetworkTransportUseSystemProxy)); set => this.SetPropertyValueOptional(value, nameof(this.NetworkTransportUseSystemProxy)); }

        /// <summary>
        /// 允许使用的 IP 网络类型
        /// </summary>
        public AllowedAddressFamily NetworkTransportAllowedAddressFamily { get => this.GetPropertyValue<AllowedAddressFamily>(); set => this.SetPropertyValue(value); }
        public bool HasNetworkTransportAllowedAddressFamily { get => this.GetPropertyHasValue(nameof(this.NetworkTransportAllowedAddressFamily)); set => this.SetPropertyHasValue<AllowedAddressFamily>(value, nameof(this.NetworkTransportAllowedAddressFamily)); }
        [JsonProperty(nameof(NetworkTransportAllowedAddressFamily)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<AllowedAddressFamily> OptionalNetworkTransportAllowedAddressFamily { get => this.GetPropertyValueOptional<AllowedAddressFamily>(nameof(this.NetworkTransportAllowedAddressFamily)); set => this.SetPropertyValueOptional(value, nameof(this.NetworkTransportAllowedAddressFamily)); }

        /// <summary>
        /// 自定义脚本
        /// </summary>
        public string? UserScript { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasUserScript { get => this.GetPropertyHasValue(nameof(this.UserScript)); set => this.SetPropertyHasValue<string>(value, nameof(this.UserScript)); }
        [JsonProperty(nameof(UserScript)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalUserScript { get => this.GetPropertyValueOptional<string>(nameof(this.UserScript)); set => this.SetPropertyValueOptional(value, nameof(this.UserScript)); }

    }

    public sealed partial class DefaultConfig
    {
        public static readonly DefaultConfig Instance = new DefaultConfig();
        private DefaultConfig() { }

        public RecordMode RecordMode => RecordMode.Standard;

        public CuttingMode CuttingMode => CuttingMode.Disabled;

        public uint CuttingNumber => 100;

        public bool RecordDanmaku => false;

        public bool RecordDanmakuRaw => false;

        public bool RecordDanmakuSuperChat => true;

        public bool RecordDanmakuGift => false;

        public bool RecordDanmakuGuard => true;

        public string RecordingQuality => "10000";

        public string FileNameRecordTemplate => "{{ roomId }}-{{ name }}/录制-{{ roomId }}-{{ \"now\" | time_zone: \"Asia/Shanghai\" | format_date: \"yyyyMMdd-HHmmss-fff\" }}-{{ title }}.flv";

        public string WebHookUrls => string.Empty;

        public string WebHookUrlsV2 => string.Empty;

        public bool WpfShowTitleAndArea => true;

        public string Cookie => string.Empty;

        public string LiveApiHost => "https://api.live.bilibili.com";

        public uint TimingCheckInterval => 10 * 60;

        public uint TimingStreamRetry => 6 * 1000;

        public uint TimingStreamRetryNoQn => 90;

        public uint TimingStreamConnect => 5 * 1000;

        public uint TimingDanmakuRetry => 9 * 1000;

        public uint TimingWatchdogTimeout => 10 * 1000;

        public uint RecordDanmakuFlushInterval => 20;

        public bool NetworkTransportUseSystemProxy => false;

        public AllowedAddressFamily NetworkTransportAllowedAddressFamily => AllowedAddressFamily.Any;

        public string UserScript => string.Empty;

    }

}
