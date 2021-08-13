import { ConfigEntry, ConfigEntryType } from './types'

export const data: Array<ConfigEntry> = [
    {
        name: "RoomId",
        description: "房间号",
        type: "int",
        configType: "roomOnly",
        defaultValue: "default",
        // web_readonly: true,
        markdown: ""
    },
    {
        name: "AutoRecord",
        description: "自动录制",
        type: "bool",
        configType: "roomOnly",
        defaultValue: "default",
        markdown: ""
    },
    {
        name: "RecordMode",
        description: "录制模式",
        type: "RecordMode",
        configType: "room",
        defaultValue: "RecordMode.Standard",
        markdown: ""
    },
    {
        name: "CuttingMode",
        description: "自动分段模式",
        type: "CuttingMode",
        configType: "room",
        defaultValue: "CuttingMode.Disabled",
        markdown: ""
    },
    {
        name: "CuttingNumber",
        description: "自动分段数值",
        type: "uint",
        configType: "room",
        defaultValue: "100",
        markdown: "按时长分段时为分钟，按大小分段时为MiB"
    },
    {
        name: "RecordDanmaku",
        description: "弹幕录制",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: ""
    },
    {
        name: "RecordDanmakuRaw",
        description: "弹幕录制-原始数据",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: ""
    },
    {
        name: "RecordDanmakuSuperChat",
        description: "弹幕录制-SuperChat",
        type: "bool",
        configType: "room",
        defaultValue: "true",
        markdown: ""
    },
    {
        name: "RecordDanmakuGift",
        description: "弹幕录制-礼物",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: ""
    },
    {
        name: "RecordDanmakuGuard",
        description: "弹幕录制-上船",
        type: "bool",
        configType: "room",
        defaultValue: "true",
        markdown: ""
    },
    {
        name: "RecordingQuality",
        type: "string?",
        configType: "room",
        description: "直播画质",
        defaultValue: "\"10000\"",
        defaultValueDescription: "10000",
        markdown: "录制的直播画质 qn 值，逗号分割，靠前的优先"
    },
    {
        name: "RecordFilenameFormat",
        description: "录制文件名格式",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "@\"{roomid}-{name}/录制-{roomid}-{date}-{time}-{ms}-{title}.flv\"",
        markdown: ""
    },
    {
        name: "WebHookUrls",
        description: "WebhookV1",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "string.Empty",
        xmlComment: "录制文件写入结束 Webhook 地址 每行一个",
        markdown: ""
    },
    {
        name: "WebHookUrlsV2",
        description: "WebhookV2",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "string.Empty",
        xmlComment: "Webhook v2 地址 每行一个",
        markdown: ""
    },
    {
        name: "WpfShowTitleAndArea",
        description: "在界面显示标题和分区",
        type: "bool",
        configType: "globalOnly",
        defaultValue: "true",
        markdown: ""
    },
    {
        name: "Cookie",
        description: "请求 API 时使用的 Cookie",
        type: "string?",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "string.Empty",
        defaultValueDescription: "（空字符串）",
        markdown: ""
    },
    {
        name: "LiveApiHost",
        description: "请求的 API Host",
        type: "string?",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "\"https://api.live.bilibili.com\"",
        xmlComment: "替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制",
        markdown: ""
    },
    {
        name: "TimingCheckInterval",
        description: "HTTP API 检查时间间隔 秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "10 * 60",
        defaultValueDescription: "600 (10分)",
        markdown: ""
    },
    {
        name: "TimingStreamRetry",
        description: "录制断开重连时间间隔 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "6 * 1000",
        defaultValueDescription: "6000 (6秒)",
        markdown: ""
    },
    {
        name: "TimingStreamConnect",
        description: "连接直播服务器超时时间 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "5 * 1000",
        defaultValueDescription: "5000 (5秒)",
        markdown: ""
    },
    {
        name: "TimingDanmakuRetry",
        description: "弹幕服务器重连时间间隔 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "9 * 1000",
        defaultValueDescription: "9000 (9秒)",
        markdown: ""
    },
    {
        name: "TimingWatchdogTimeout",
        description: "最大允许未收到直播数据时间 毫秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "10 * 1000",
        defaultValueDescription: "10000 (10秒)",
        markdown: ""
    },
    {
        name: "RecordDanmakuFlushInterval",
        description: "触发刷新弹幕写入缓冲的个数",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "20",
        xmlComment: "触发 <see cref=\"System.Xml.XmlWriter.Flush\"/> 的弹幕个数",
        markdown: ""
    },
];
