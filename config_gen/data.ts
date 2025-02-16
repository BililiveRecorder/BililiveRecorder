import { ConfigEntry } from './types'

export const data: Array<ConfigEntry> = [
    {
        id: "RoomId",
        name: "房间号",
        type: "int",
        configType: "roomOnly",
        default: "",
        webReadonly: true
    },
    {
        id: "AutoRecord",
        name: "自动录制",
        type: "bool",
        configType: "roomOnly",
        default: ""
    },
    {
        id: "RecordMode",
        name: "录制模式",
        type: "RecordMode",
        configType: "room",
        default: "RecordMode.Standard"
    },
    {
        id: "CuttingMode",
        name: "自动分段模式",
        type: "CuttingMode",
        configType: "room",
        default: "CuttingMode.Disabled"
    },
    {
        id: "CuttingNumber",
        name: "自动分段数值",
        type: "uint",
        configType: "room",
        default: 100
    },
    {
        id: "CuttingByTitle",
        name: "改标题后自动分段",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "RecordDanmaku",
        name: "弹幕录制",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "RecordDanmakuRaw",
        name: "弹幕录制-原始数据",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "RecordDanmakuSuperChat",
        name: "弹幕录制-SuperChat",
        type: "bool",
        configType: "room",
        default: true
    },
    {
        id: "RecordDanmakuGift",
        name: "弹幕录制-礼物",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "RecordDanmakuGuard",
        name: "弹幕录制-上船",
        type: "bool",
        configType: "room",
        default: true
    },
    {
        id: "SaveStreamCover",
        name: "保存直播封面",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "RecordingQuality",
        name: "直播画质",
        type: "string?",
        configType: "room",
        default: "avc10000,hevc10000",
    },
    {
        id: "FileNameRecordTemplate",
        name: "录制文件名模板",
        type: "string?",
        configType: "globalOnly",
        default: '{{ roomId }}-{{ name }}/录制-{{ roomId }}-{{ "now" | time_zone: "Asia/Shanghai" | format_date: "yyyyMMdd-HHmmss-fff" }}-{{ title }}.flv',
    },
    {
        id: "FlvProcessorSplitOnScriptTag",
        name: "FLV修复-检测到可能缺少数据时分段",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "FlvProcessorDisableSplitOnH264AnnexB",
        name: "FLV修复-检测到 H264 Annex-B 时禁用修复分段",
        type: "bool",
        configType: "room",
        default: false
    },
    {
        id: "FlvWriteMetadata",
        name: "是否在视频文件写入直播信息 metadata",
        type: "bool",
        configType: "globalOnly",
        advancedConfig: true,
        default: true
    },
    {
        id: "TitleFilterPatterns",
        name: "不录制的标题匹配正则",
        type: "string?",
        configType: "room",
        default: ""
    },
    {
        id: "WebHookUrls",
        name: "WebhookV1",
        type: "string?",
        configType: "globalOnly",
        default: "",
    },
    {
        id: "WebHookUrlsV2",
        name: "WebhookV2",
        type: "string?",
        configType: "globalOnly",
        default: "",
    },
    {
        id: "WpfShowTitleAndArea",
        name: "桌面版在界面显示标题和分区",
        type: "bool",
        configType: "globalOnly",
        default: true
    },
    {
        id: "WpfNotifyStreamStart",
        name: "桌面版开播时弹出系统通知",
        type: "bool",
        configType: "globalOnly",
        default: false
    },
    {
        id: "Cookie",
        name: "Cookie",
        type: "string?",
        configType: "globalOnly",
        advancedConfig: true,
        default: "",
    },
    {
        id: "LiveApiHost",
        name: "API Host",
        type: "string?",
        configType: "globalOnly",
        advancedConfig: true,
        default: "https://api.live.bilibili.com",
    },
    {
        id: "TimingCheckInterval",
        name: "主动检查时间间隔 秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 3 * 60,
    },
    {
        id: "TimingApiTimeout",
        name: "请求mikufansAPI超时时间 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 10 * 1000,
    },
    {
        id: "TimingStreamRetry",
        name: "录制断开重连时间间隔 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 6 * 1000,
    },
    {
        id: "TimingStreamRetryNoQn",
        name: "录制无指定画质重连时间间隔 秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 90,
    },
    {
        id: "TimingStreamConnect",
        name: "连接直播服务器超时时间 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 5 * 1000,
    },
    {
        id: "TimingDanmakuRetry",
        name: "弹幕服务器重连时间间隔 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 9 * 1000,
    },
    {
        id: "TimingWatchdogTimeout",
        name: "最大未收到直播数据时间 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 10 * 1000,
    },
    {
        id: "RecordDanmakuFlushInterval",
        name: "触发刷新弹幕写入缓冲的个数",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        default: 20,
    },
    {
        id: "DanmakuTransport",
        name: "使用的弹幕服务器传输协议",
        type: "DanmakuTransportMode",
        configType: "globalOnly",
        advancedConfig: true,
        default: "DanmakuTransportMode.Wss",
    },
    {
        id: "DanmakuAuthenticateWithStreamerUid",
        name: "使用直播间主播的uid进行弹幕服务器认证",
        type: "bool",
        configType: "globalOnly",
        advancedConfig: true,
        default: false,
    },
    {
        id: "NetworkTransportUseSystemProxy",
        name: "是否使用系统代理",
        type: "bool",
        default: false,
        configType: "globalOnly",
        advancedConfig: true
    },
    {
        id: "NetworkTransportAllowedAddressFamily",
        name: "允许使用的 IP 网络类型",
        type: "AllowedAddressFamily",
        default: "AllowedAddressFamily.Any",
        configType: "globalOnly",
        advancedConfig: true
    },
    {
        id: "UserScript",
        name: "自定义脚本",
        type: "string?",
        default: "",
        configType: "globalOnly",
        advancedConfig: true
    },
];
