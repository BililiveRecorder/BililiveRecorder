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
        /// 改标题后自动分段
        /// </summary>
        public bool CuttingByTitle { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingByTitle { get => this.GetPropertyHasValue(nameof(this.CuttingByTitle)); set => this.SetPropertyHasValue<bool>(value, nameof(this.CuttingByTitle)); }
        [JsonProperty(nameof(CuttingByTitle)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalCuttingByTitle { get => this.GetPropertyValueOptional<bool>(nameof(this.CuttingByTitle)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingByTitle)); }

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
        /// 保存直播封面
        /// </summary>
        public bool SaveStreamCover { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasSaveStreamCover { get => this.GetPropertyHasValue(nameof(this.SaveStreamCover)); set => this.SetPropertyHasValue<bool>(value, nameof(this.SaveStreamCover)); }
        [JsonProperty(nameof(SaveStreamCover)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalSaveStreamCover { get => this.GetPropertyValueOptional<bool>(nameof(this.SaveStreamCover)); set => this.SetPropertyValueOptional(value, nameof(this.SaveStreamCover)); }

        /// <summary>
        /// 直播画质
        /// </summary>
        public string? RecordingQuality { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasRecordingQuality { get => this.GetPropertyHasValue(nameof(this.RecordingQuality)); set => this.SetPropertyHasValue<string>(value, nameof(this.RecordingQuality)); }
        [JsonProperty(nameof(RecordingQuality)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalRecordingQuality { get => this.GetPropertyValueOptional<string>(nameof(this.RecordingQuality)); set => this.SetPropertyValueOptional(value, nameof(this.RecordingQuality)); }

        /// <summary>
        /// FLV修复-检测到可能缺少数据时分段
        /// </summary>
        public bool FlvProcessorSplitOnScriptTag { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasFlvProcessorSplitOnScriptTag { get => this.GetPropertyHasValue(nameof(this.FlvProcessorSplitOnScriptTag)); set => this.SetPropertyHasValue<bool>(value, nameof(this.FlvProcessorSplitOnScriptTag)); }
        [JsonProperty(nameof(FlvProcessorSplitOnScriptTag)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalFlvProcessorSplitOnScriptTag { get => this.GetPropertyValueOptional<bool>(nameof(this.FlvProcessorSplitOnScriptTag)); set => this.SetPropertyValueOptional(value, nameof(this.FlvProcessorSplitOnScriptTag)); }

        /// <summary>
        /// FLV修复-检测到 H264 Annex-B 时禁用修复分段
        /// </summary>
        public bool FlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasFlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyHasValue(nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); set => this.SetPropertyHasValue<bool>(value, nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); }
        [JsonProperty(nameof(FlvProcessorDisableSplitOnH264AnnexB)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalFlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyValueOptional<bool>(nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); set => this.SetPropertyValueOptional(value, nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); }

        /// <summary>
        /// 不录制的标题匹配正则
        /// </summary>
        public string? TitleFilterPatterns { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasTitleFilterPatterns { get => this.GetPropertyHasValue(nameof(this.TitleFilterPatterns)); set => this.SetPropertyHasValue<string>(value, nameof(this.TitleFilterPatterns)); }
        [JsonProperty(nameof(TitleFilterPatterns)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalTitleFilterPatterns { get => this.GetPropertyValueOptional<string>(nameof(this.TitleFilterPatterns)); set => this.SetPropertyValueOptional(value, nameof(this.TitleFilterPatterns)); }

        /// <summary>
        /// 录制文件名模板
        /// </summary>
        public string? FileNameRecordTemplate => this.GetPropertyValue<string>();

        /// <summary>
        /// 是否在视频文件写入直播信息 metadata
        /// </summary>
        public bool FlvWriteMetadata => this.GetPropertyValue<bool>();

        /// <summary>
        /// WebhookV1
        /// </summary>
        public string? WebHookUrls => this.GetPropertyValue<string>();

        /// <summary>
        /// WebhookV2
        /// </summary>
        public string? WebHookUrlsV2 => this.GetPropertyValue<string>();

        /// <summary>
        /// 桌面版在界面显示标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea => this.GetPropertyValue<bool>();

        /// <summary>
        /// 桌面版开播时弹出系统通知
        /// </summary>
        public bool WpfNotifyStreamStart => this.GetPropertyValue<bool>();

        /// <summary>
        /// Cookie
        /// </summary>
        public string? Cookie => this.GetPropertyValue<string>();

        /// <summary>
        /// API Host
        /// </summary>
        public string? LiveApiHost => this.GetPropertyValue<string>();

        /// <summary>
        /// 主动检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 请求mikufansAPI超时时间 毫秒
        /// </summary>
        public uint TimingApiTimeout => this.GetPropertyValue<uint>();

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
        /// 最大未收到直播数据时间 毫秒
        /// </summary>
        public uint TimingWatchdogTimeout => this.GetPropertyValue<uint>();

        /// <summary>
        /// 触发刷新弹幕写入缓冲的个数
        /// </summary>
        public uint RecordDanmakuFlushInterval => this.GetPropertyValue<uint>();

        /// <summary>
        /// 使用的弹幕服务器传输协议
        /// </summary>
        public DanmakuTransportMode DanmakuTransport => this.GetPropertyValue<DanmakuTransportMode>();

        /// <summary>
        /// 使用直播间主播的uid进行弹幕服务器认证
        /// </summary>
        public bool DanmakuAuthenticateWithStreamerUid => this.GetPropertyValue<bool>();

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
        /// 改标题后自动分段
        /// </summary>
        public bool CuttingByTitle { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasCuttingByTitle { get => this.GetPropertyHasValue(nameof(this.CuttingByTitle)); set => this.SetPropertyHasValue<bool>(value, nameof(this.CuttingByTitle)); }
        [JsonProperty(nameof(CuttingByTitle)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalCuttingByTitle { get => this.GetPropertyValueOptional<bool>(nameof(this.CuttingByTitle)); set => this.SetPropertyValueOptional(value, nameof(this.CuttingByTitle)); }

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
        /// 保存直播封面
        /// </summary>
        public bool SaveStreamCover { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasSaveStreamCover { get => this.GetPropertyHasValue(nameof(this.SaveStreamCover)); set => this.SetPropertyHasValue<bool>(value, nameof(this.SaveStreamCover)); }
        [JsonProperty(nameof(SaveStreamCover)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalSaveStreamCover { get => this.GetPropertyValueOptional<bool>(nameof(this.SaveStreamCover)); set => this.SetPropertyValueOptional(value, nameof(this.SaveStreamCover)); }

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
        /// FLV修复-检测到可能缺少数据时分段
        /// </summary>
        public bool FlvProcessorSplitOnScriptTag { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasFlvProcessorSplitOnScriptTag { get => this.GetPropertyHasValue(nameof(this.FlvProcessorSplitOnScriptTag)); set => this.SetPropertyHasValue<bool>(value, nameof(this.FlvProcessorSplitOnScriptTag)); }
        [JsonProperty(nameof(FlvProcessorSplitOnScriptTag)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalFlvProcessorSplitOnScriptTag { get => this.GetPropertyValueOptional<bool>(nameof(this.FlvProcessorSplitOnScriptTag)); set => this.SetPropertyValueOptional(value, nameof(this.FlvProcessorSplitOnScriptTag)); }

        /// <summary>
        /// FLV修复-检测到 H264 Annex-B 时禁用修复分段
        /// </summary>
        public bool FlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasFlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyHasValue(nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); set => this.SetPropertyHasValue<bool>(value, nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); }
        [JsonProperty(nameof(FlvProcessorDisableSplitOnH264AnnexB)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalFlvProcessorDisableSplitOnH264AnnexB { get => this.GetPropertyValueOptional<bool>(nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); set => this.SetPropertyValueOptional(value, nameof(this.FlvProcessorDisableSplitOnH264AnnexB)); }

        /// <summary>
        /// 是否在视频文件写入直播信息 metadata
        /// </summary>
        public bool FlvWriteMetadata { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasFlvWriteMetadata { get => this.GetPropertyHasValue(nameof(this.FlvWriteMetadata)); set => this.SetPropertyHasValue<bool>(value, nameof(this.FlvWriteMetadata)); }
        [JsonProperty(nameof(FlvWriteMetadata)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalFlvWriteMetadata { get => this.GetPropertyValueOptional<bool>(nameof(this.FlvWriteMetadata)); set => this.SetPropertyValueOptional(value, nameof(this.FlvWriteMetadata)); }

        /// <summary>
        /// 不录制的标题匹配正则
        /// </summary>
        public string? TitleFilterPatterns { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasTitleFilterPatterns { get => this.GetPropertyHasValue(nameof(this.TitleFilterPatterns)); set => this.SetPropertyHasValue<string>(value, nameof(this.TitleFilterPatterns)); }
        [JsonProperty(nameof(TitleFilterPatterns)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalTitleFilterPatterns { get => this.GetPropertyValueOptional<string>(nameof(this.TitleFilterPatterns)); set => this.SetPropertyValueOptional(value, nameof(this.TitleFilterPatterns)); }

        /// <summary>
        /// WebhookV1
        /// </summary>
        public string? WebHookUrls { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasWebHookUrls { get => this.GetPropertyHasValue(nameof(this.WebHookUrls)); set => this.SetPropertyHasValue<string>(value, nameof(this.WebHookUrls)); }
        [JsonProperty(nameof(WebHookUrls)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalWebHookUrls { get => this.GetPropertyValueOptional<string>(nameof(this.WebHookUrls)); set => this.SetPropertyValueOptional(value, nameof(this.WebHookUrls)); }

        /// <summary>
        /// WebhookV2
        /// </summary>
        public string? WebHookUrlsV2 { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasWebHookUrlsV2 { get => this.GetPropertyHasValue(nameof(this.WebHookUrlsV2)); set => this.SetPropertyHasValue<string>(value, nameof(this.WebHookUrlsV2)); }
        [JsonProperty(nameof(WebHookUrlsV2)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalWebHookUrlsV2 { get => this.GetPropertyValueOptional<string>(nameof(this.WebHookUrlsV2)); set => this.SetPropertyValueOptional(value, nameof(this.WebHookUrlsV2)); }

        /// <summary>
        /// 桌面版在界面显示标题和分区
        /// </summary>
        public bool WpfShowTitleAndArea { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasWpfShowTitleAndArea { get => this.GetPropertyHasValue(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyHasValue<bool>(value, nameof(this.WpfShowTitleAndArea)); }
        [JsonProperty(nameof(WpfShowTitleAndArea)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalWpfShowTitleAndArea { get => this.GetPropertyValueOptional<bool>(nameof(this.WpfShowTitleAndArea)); set => this.SetPropertyValueOptional(value, nameof(this.WpfShowTitleAndArea)); }

        /// <summary>
        /// 桌面版开播时弹出系统通知
        /// </summary>
        public bool WpfNotifyStreamStart { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasWpfNotifyStreamStart { get => this.GetPropertyHasValue(nameof(this.WpfNotifyStreamStart)); set => this.SetPropertyHasValue<bool>(value, nameof(this.WpfNotifyStreamStart)); }
        [JsonProperty(nameof(WpfNotifyStreamStart)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalWpfNotifyStreamStart { get => this.GetPropertyValueOptional<bool>(nameof(this.WpfNotifyStreamStart)); set => this.SetPropertyValueOptional(value, nameof(this.WpfNotifyStreamStart)); }

        /// <summary>
        /// Cookie
        /// </summary>
        public string? Cookie { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasCookie { get => this.GetPropertyHasValue(nameof(this.Cookie)); set => this.SetPropertyHasValue<string>(value, nameof(this.Cookie)); }
        [JsonProperty(nameof(Cookie)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalCookie { get => this.GetPropertyValueOptional<string>(nameof(this.Cookie)); set => this.SetPropertyValueOptional(value, nameof(this.Cookie)); }

        /// <summary>
        /// API Host
        /// </summary>
        public string? LiveApiHost { get => this.GetPropertyValue<string>(); set => this.SetPropertyValue(value); }
        public bool HasLiveApiHost { get => this.GetPropertyHasValue(nameof(this.LiveApiHost)); set => this.SetPropertyHasValue<string>(value, nameof(this.LiveApiHost)); }
        [JsonProperty(nameof(LiveApiHost)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<string?> OptionalLiveApiHost { get => this.GetPropertyValueOptional<string>(nameof(this.LiveApiHost)); set => this.SetPropertyValueOptional(value, nameof(this.LiveApiHost)); }

        /// <summary>
        /// 主动检查时间间隔 秒
        /// </summary>
        public uint TimingCheckInterval { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingCheckInterval { get => this.GetPropertyHasValue(nameof(this.TimingCheckInterval)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingCheckInterval)); }
        [JsonProperty(nameof(TimingCheckInterval)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingCheckInterval { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingCheckInterval)); set => this.SetPropertyValueOptional(value, nameof(this.TimingCheckInterval)); }

        /// <summary>
        /// 请求mikufansAPI超时时间 毫秒
        /// </summary>
        public uint TimingApiTimeout { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingApiTimeout { get => this.GetPropertyHasValue(nameof(this.TimingApiTimeout)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingApiTimeout)); }
        [JsonProperty(nameof(TimingApiTimeout)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingApiTimeout { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingApiTimeout)); set => this.SetPropertyValueOptional(value, nameof(this.TimingApiTimeout)); }

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
        /// 最大未收到直播数据时间 毫秒
        /// </summary>
        public uint TimingWatchdogTimeout { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasTimingWatchdogTimeout { get => this.GetPropertyHasValue(nameof(this.TimingWatchdogTimeout)); set => this.SetPropertyHasValue<uint>(value, nameof(this.TimingWatchdogTimeout)); }
        [JsonProperty(nameof(TimingWatchdogTimeout)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalTimingWatchdogTimeout { get => this.GetPropertyValueOptional<uint>(nameof(this.TimingWatchdogTimeout)); set => this.SetPropertyValueOptional(value, nameof(this.TimingWatchdogTimeout)); }

        /// <summary>
        /// 触发刷新弹幕写入缓冲的个数
        /// </summary>
        public uint RecordDanmakuFlushInterval { get => this.GetPropertyValue<uint>(); set => this.SetPropertyValue(value); }
        public bool HasRecordDanmakuFlushInterval { get => this.GetPropertyHasValue(nameof(this.RecordDanmakuFlushInterval)); set => this.SetPropertyHasValue<uint>(value, nameof(this.RecordDanmakuFlushInterval)); }
        [JsonProperty(nameof(RecordDanmakuFlushInterval)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<uint> OptionalRecordDanmakuFlushInterval { get => this.GetPropertyValueOptional<uint>(nameof(this.RecordDanmakuFlushInterval)); set => this.SetPropertyValueOptional(value, nameof(this.RecordDanmakuFlushInterval)); }

        /// <summary>
        /// 使用的弹幕服务器传输协议
        /// </summary>
        public DanmakuTransportMode DanmakuTransport { get => this.GetPropertyValue<DanmakuTransportMode>(); set => this.SetPropertyValue(value); }
        public bool HasDanmakuTransport { get => this.GetPropertyHasValue(nameof(this.DanmakuTransport)); set => this.SetPropertyHasValue<DanmakuTransportMode>(value, nameof(this.DanmakuTransport)); }
        [JsonProperty(nameof(DanmakuTransport)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<DanmakuTransportMode> OptionalDanmakuTransport { get => this.GetPropertyValueOptional<DanmakuTransportMode>(nameof(this.DanmakuTransport)); set => this.SetPropertyValueOptional(value, nameof(this.DanmakuTransport)); }

        /// <summary>
        /// 使用直播间主播的uid进行弹幕服务器认证
        /// </summary>
        public bool DanmakuAuthenticateWithStreamerUid { get => this.GetPropertyValue<bool>(); set => this.SetPropertyValue(value); }
        public bool HasDanmakuAuthenticateWithStreamerUid { get => this.GetPropertyHasValue(nameof(this.DanmakuAuthenticateWithStreamerUid)); set => this.SetPropertyHasValue<bool>(value, nameof(this.DanmakuAuthenticateWithStreamerUid)); }
        [JsonProperty(nameof(DanmakuAuthenticateWithStreamerUid)), EditorBrowsable(EditorBrowsableState.Never)]
        public Optional<bool> OptionalDanmakuAuthenticateWithStreamerUid { get => this.GetPropertyValueOptional<bool>(nameof(this.DanmakuAuthenticateWithStreamerUid)); set => this.SetPropertyValueOptional(value, nameof(this.DanmakuAuthenticateWithStreamerUid)); }

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

        public bool CuttingByTitle => false;

        public bool RecordDanmaku => false;

        public bool RecordDanmakuRaw => false;

        public bool RecordDanmakuSuperChat => true;

        public bool RecordDanmakuGift => false;

        public bool RecordDanmakuGuard => true;

        public bool SaveStreamCover => false;

        public string RecordingQuality => @"avc10000,hevc10000";

        public string FileNameRecordTemplate => @"{{ roomId }}-{{ name }}/录制-{{ roomId }}-{{ ""now"" | time_zone: ""Asia/Shanghai"" | format_date: ""yyyyMMdd-HHmmss-fff"" }}-{{ title }}.flv";

        public bool FlvProcessorSplitOnScriptTag => false;

        public bool FlvProcessorDisableSplitOnH264AnnexB => false;

        public bool FlvWriteMetadata => true;

        public string TitleFilterPatterns => @"";

        public string WebHookUrls => @"";

        public string WebHookUrlsV2 => @"";

        public bool WpfShowTitleAndArea => true;

        public bool WpfNotifyStreamStart => false;

        public string Cookie => @"";

        public string LiveApiHost => @"https://api.live.bilibili.com";

        public uint TimingCheckInterval => 180;

        public uint TimingApiTimeout => 10000;

        public uint TimingStreamRetry => 6000;

        public uint TimingStreamRetryNoQn => 90;

        public uint TimingStreamConnect => 5000;

        public uint TimingDanmakuRetry => 9000;

        public uint TimingWatchdogTimeout => 10000;

        public uint RecordDanmakuFlushInterval => 20;

        public DanmakuTransportMode DanmakuTransport => DanmakuTransportMode.Wss;

        public bool DanmakuAuthenticateWithStreamerUid => false;

        public bool NetworkTransportUseSystemProxy => false;

        public AllowedAddressFamily NetworkTransportAllowedAddressFamily => AllowedAddressFamily.Any;

        public string UserScript => @"";

    }

}
