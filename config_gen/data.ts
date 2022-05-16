import { ConfigEntry, ConfigEntryType } from './types'

export const data: Array<ConfigEntry> = [
    {
        name: "RoomId",
        description: "房间号",
        type: "int",
        configType: "roomOnly",
        defaultValue: "default",
        webReadonly: true,
        markdown: ""
    },
    {
        name: "AutoRecord",
        description: "自动录制",
        type: "bool",
        configType: "roomOnly",
        defaultValue: "default",
        markdown: "设为 `true` 为启用自动录制，`false` 为不自动录制。"
    },
    {
        name: "RecordMode",
        description: "录制模式",
        type: "RecordMode",
        configType: "room",
        defaultValue: "RecordMode.Standard",
        markdown: "本设置项是一个 enum，键值对应如下：\n\n| 键 | 值 |\n|:--:|:--:|\n| RecordMode.Standard | 0 |\n| RecordMode.RawData | 1 |\n\n关于录制模式的说明见 [录制模式](/docs/basic/record_mode/)"
    },
    {
        name: "CuttingMode",
        description: "自动分段模式",
        type: "CuttingMode",
        configType: "room",
        defaultValue: "CuttingMode.Disabled",
        markdown: "本设置项是一个 enum，键值对应如下：\n\n| 键 | 值 |\n|:--:|:--:|\n| CuttingMode.Disabled | 0 |\n| CuttingMode.ByTime | 1 |\n| CuttingMode.BySize | 2 |"
    },
    {
        name: "CuttingNumber",
        description: "自动分段数值",
        type: "uint",
        configType: "room",
        defaultValue: "100",
        markdown: "根据 CuttingMode 设置的不同：    \n当按时长分段时，本设置的单位为分钟。  \n当按大小分段时，本设置的单位为MiB。"
    },
    {
        name: "RecordDanmaku",
        description: "弹幕录制",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: "是否录制弹幕，`true` 为录制，`false` 为不录制。\n\n本设置同时是所有“弹幕录制”的总开关，当本设置为 `false` 时其他所有“弹幕录制”设置无效，不会写入弹幕XML文件。"
    },
    {
        name: "RecordDanmakuRaw",
        description: "弹幕录制-原始数据",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: "是否记录原始 JSON 数据。\n\n弹幕原始数据会保存到 XML 文件每一条弹幕数据的 `raw` attribute 上。\n\n当 `RecordDanmaku` 为 `false` 时本项设置无效。"
    },
    {
        name: "RecordDanmakuSuperChat",
        description: "弹幕录制-SuperChat",
        type: "bool",
        configType: "room",
        defaultValue: "true",
        markdown: "是否记录 SuperChat。\n\n当 `RecordDanmaku` 为 `false` 时本项设置无效。"
    },
    {
        name: "RecordDanmakuGift",
        description: "弹幕录制-礼物",
        type: "bool",
        configType: "room",
        defaultValue: "false",
        markdown: "是否记录礼物。\n\n当 `RecordDanmaku` 为 `false` 时本项设置无效。"
    },
    {
        name: "RecordDanmakuGuard",
        description: "弹幕录制-上船",
        type: "bool",
        configType: "room",
        defaultValue: "true",
        markdown: "是否记录上船（购买舰长）。\n\n当 `RecordDanmaku` 为 `false` 时本项设置无效。"
    },
    {
        name: "RecordingQuality",
        type: "string?",
        configType: "room",
        description: "直播画质",
        defaultValue: "\"10000\"",
        defaultValueDescription: "10000",
        markdown: "录制的直播画质 qn 值，以英文逗号分割，靠前的优先。\n\n**注意**（从录播姬 1.3.10 开始）：\n\n- 所有主播刚开播时都是只有“原画”的，如果选择不录原画会导致直播开头漏录。\n- 如果设置的录制画质里没有原画，但是主播只有原画画质，会导致不能录制直播。\n- 录播姬不会为了切换录制的画质主动断开录制。\n\n画质 | qn 值\n:--:|:--:\n4K | 20000\n原画 | 10000\n蓝光(杜比) | 401\n蓝光 | 400\n超清 | 250\n高清 | 150\n流畅 | 80"
    },
    {
        name: "FileNameRecordTemplate",
        description: "录制文件名模板",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "\"{{ roomId }}-{{ name }}/录制-{{ roomId }}-{{ \\\"now\\\" | time_zone: \\\"Asia/Shanghai\\\" | format_date: \\\"yyyyMMdd-HHmmss-fff\\\" }}-{{ title }}.flv\"",
        defaultValueDescription: "\"{{ roomId }}-{{ name }}/录制-{{ roomId }}-{{ \"now\" | time_zone: \"Asia/Shanghai\" | format_date: \"yyyyMMdd-HHmmss-fff\" }}-{{ title }}.flv\"",
        markdown: "TODO: config v3 新的文件名模板系统的文档还没有写"
    },
    {
        name: "WebHookUrls",
        description: "WebhookV1",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "string.Empty",
        xmlComment: "录制文件写入结束 Webhook 地址 每行一个",
        markdown: "具体文档见 [Webhook](/docs/basic/webhook/)"
    },
    {
        name: "WebHookUrlsV2",
        description: "WebhookV2",
        type: "string?",
        configType: "globalOnly",
        defaultValue: "string.Empty",
        xmlComment: "Webhook v2 地址 每行一个",
        markdown: "具体文档见 [Webhook](/docs/basic/webhook/)"
    },
    {
        name: "WpfShowTitleAndArea",
        description: "在界面显示标题和分区",
        type: "bool",
        configType: "globalOnly",
        defaultValue: "true",
        markdown: "只在桌面版（WPF版）有效"
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
        name: "TimingStreamRetryNoQn",
        description: "录制无指定画质重连时间间隔 秒",
        type: "uint",
        configType: "globalOnly",
        advancedConfig: true,
        defaultValue: "90",
        defaultValueDescription: "90 (1.5分钟)",
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
    {
        name: "NetworkTransportUseSystemProxy",
        description: "是否使用系统代理",
        type: "bool",
        defaultValue: "false",
        configType: "globalOnly",
        advancedConfig: true,
        markdown: ""
    },
    {
        name: "NetworkTransportAllowedAddressFamily",
        description: "允许使用的 IP 网络类型",
        type: "AllowedAddressFamily",
        defaultValue: "AllowedAddressFamily.Any",
        configType: "globalOnly",
        advancedConfig: true,
        markdown: ""
    },
    {
        name: "UserScript",
        description: "自定义脚本",
        type: "string?",
        defaultValue: "string.Empty",
        configType: "globalOnly",
        advancedConfig: true,
        markdown: ""
    },
];
