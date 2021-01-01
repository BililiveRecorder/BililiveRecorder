module.exports = {
    "global": [{
        "name": "EnabledFeature",
        "type": "EnabledFeature",
        "desc": "启用的功能",
        "default": "EnabledFeature.RecordOnly"
    }, {
        "name": "ClipLengthPast",
        "type": "uint",
        "desc": "剪辑-过去的时长(秒)",
        "default": "20"
    }, {
        "name": "ClipLengthFuture",
        "type": "uint",
        "desc": "剪辑-将来的时长(秒)",
        "default": "10"
    }, {
        "name": "TimingStreamRetry",
        "type": "uint",
        "desc": "录制断开重连时间间隔 毫秒",
        "default": "6 * 1000"
    }, {
        "name": "TimingStreamConnect",
        "type": "uint",
        "desc": "连接直播服务器超时时间 毫秒",
        "default": "5 * 1000"
    }, {
        "name": "TimingDanmakuRetry",
        "type": "uint",
        "desc": "弹幕服务器重连时间间隔 毫秒",
        "default": "15 * 1000"
    }, {
        "name": "TimingCheckInterval",
        "type": "uint",
        "desc": "HTTP API 检查时间间隔 秒",
        "default": "5 * 60"
    }, {
        "name": "TimingWatchdogTimeout",
        "type": "uint",
        "desc": "最大未收到新直播数据时间 毫秒",
        "default": "10 * 1000"
    }, {
        "name": "RecordDanmakuFlushInterval",
        "type": "uint",
        "desc": "触发 <see cref=\"System.Xml.XmlWriter.Flush\"/> 的弹幕个数",
        "default": "20"
    }, {
        "name": "Cookie",
        "type": "string",
        "desc": "请求 API 时使用的 Cookie",
        "default": "string.Empty",
        "nullable": true
    }, {
        "name": "WebHookUrls",
        "type": "string",
        "desc": "录制文件写入结束 Webhook 地址 每行一个",
        "default": "string.Empty",
        "nullable": true
    }, {
        "name": "LiveApiHost",
        "type": "string",
        "desc": "替换 api.live.bilibili.com 服务器为其他反代，可以支持在云服务器上录制",
        "default": "\"https://api.live.bilibili.com\"",
        "nullable": true
    }, {
        "name": "RecordFilenameFormat",
        "type": "string",
        "desc": "录制文件名模板",
        "default": "@\"{roomid}-{name}/录制-{roomid}-{date}-{time}-{title}.flv\"",
        "nullable": true
    }, {
        "name": "ClipFilenameFormat",
        "type": "string",
        "desc": "剪辑文件名模板",
        "default": "@\"{roomid}-{name}/剪辑片段-{roomid}-{date}-{time}-{title}.flv\"",
        "nullable": true
    }, ],
    "room": [{
        "name": "RoomId",
        "type": "int",
        "desc": "房间号",
        "default": "default",
        "without_global": true
    }, {
        "name": "AutoRecord",
        "type": "bool",
        "desc": "是否启用自动录制",
        "default": "default",
        "without_global": true
    }, {
        "name": "CuttingMode",
        "type": "AutoCuttingMode",
        "desc": "录制文件自动切割模式",
        "default": "AutoCuttingMode.Disabled"
    }, {
        "name": "CuttingNumber",
        "type": "uint",
        "desc": "录制文件自动切割数值（分钟/MiB）",
        "default": "100"
    }, {
        "name": "RecordDanmaku",
        "type": "bool",
        "desc": "是否同时录制弹幕",
        "default": "false"
    }, {
        "name": "RecordDanmakuRaw",
        "type": "bool",
        "desc": "是否记录弹幕原始数据",
        "default": "false"
    }, {
        "name": "RecordDanmakuSuperChat",
        "type": "bool",
        "desc": "是否同时录制 SuperChat",
        "default": "true"
    }, {
        "name": "RecordDanmakuGift",
        "type": "bool",
        "desc": "是否同时录制 礼物",
        "default": "false"
    }, {
        "name": "RecordDanmakuGuard",
        "type": "bool",
        "desc": "是否同时录制 上船",
        "default": "true"
    }, ]
}
