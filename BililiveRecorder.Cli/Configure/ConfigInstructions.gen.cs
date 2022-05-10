// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE /config_gen/README.md
// ******************************

using System.Collections.Generic;
using System.ComponentModel;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;

namespace BililiveRecorder.Cli.Configure
{
    public enum GlobalConfigProperties
    {
        [Description("[grey]Exit[/]")]
        Exit,
        RecordMode,
        CuttingMode,
        CuttingNumber,
        RecordDanmaku,
        RecordDanmakuRaw,
        RecordDanmakuSuperChat,
        RecordDanmakuGift,
        RecordDanmakuGuard,
        RecordingQuality,
        FileNameRecordTemplate,
        WebHookUrls,
        WebHookUrlsV2,
        WpfShowTitleAndArea,
        Cookie,
        LiveApiHost,
        TimingCheckInterval,
        TimingStreamRetry,
        TimingStreamRetryNoQn,
        TimingStreamConnect,
        TimingDanmakuRetry,
        TimingWatchdogTimeout,
        RecordDanmakuFlushInterval,
        NetworkTransportUseSystemProxy,
        NetworkTransportAllowedAddressFamily,
        UserScript
    }
    public enum RoomConfigProperties
    {
        [Description("[grey]Exit[/]")]
        Exit,
        RoomId,
        AutoRecord,
        RecordMode,
        CuttingMode,
        CuttingNumber,
        RecordDanmaku,
        RecordDanmakuRaw,
        RecordDanmakuSuperChat,
        RecordDanmakuGift,
        RecordDanmakuGuard,
        RecordingQuality
    }
    public static class ConfigInstructions
    {
        public static Dictionary<GlobalConfigProperties, ConfigInstructionBase<GlobalConfig>> GlobalConfig = new();
        public static Dictionary<RoomConfigProperties, ConfigInstructionBase<RoomConfig>> RoomConfig = new();

        static ConfigInstructions()
        {
            GlobalConfig.Add(GlobalConfigProperties.RecordMode, new ConfigInstruction<GlobalConfig, RecordMode>(config => config.HasRecordMode = false, (config, value) => config.RecordMode = value) { Name = "RecordMode", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.CuttingMode, new ConfigInstruction<GlobalConfig, CuttingMode>(config => config.HasCuttingMode = false, (config, value) => config.CuttingMode = value) { Name = "CuttingMode", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.CuttingNumber, new ConfigInstruction<GlobalConfig, uint>(config => config.HasCuttingNumber = false, (config, value) => config.CuttingNumber = value) { Name = "CuttingNumber", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmaku, new ConfigInstruction<GlobalConfig, bool>(config => config.HasRecordDanmaku = false, (config, value) => config.RecordDanmaku = value) { Name = "RecordDanmaku", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmakuRaw, new ConfigInstruction<GlobalConfig, bool>(config => config.HasRecordDanmakuRaw = false, (config, value) => config.RecordDanmakuRaw = value) { Name = "RecordDanmakuRaw", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmakuSuperChat, new ConfigInstruction<GlobalConfig, bool>(config => config.HasRecordDanmakuSuperChat = false, (config, value) => config.RecordDanmakuSuperChat = value) { Name = "RecordDanmakuSuperChat", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmakuGift, new ConfigInstruction<GlobalConfig, bool>(config => config.HasRecordDanmakuGift = false, (config, value) => config.RecordDanmakuGift = value) { Name = "RecordDanmakuGift", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmakuGuard, new ConfigInstruction<GlobalConfig, bool>(config => config.HasRecordDanmakuGuard = false, (config, value) => config.RecordDanmakuGuard = value) { Name = "RecordDanmakuGuard", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordingQuality, new ConfigInstruction<GlobalConfig, string>(config => config.HasRecordingQuality = false, (config, value) => config.RecordingQuality = value) { Name = "RecordingQuality", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.FileNameRecordTemplate, new ConfigInstruction<GlobalConfig, string>(config => config.HasFileNameRecordTemplate = false, (config, value) => config.FileNameRecordTemplate = value) { Name = "FileNameRecordTemplate", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.WebHookUrls, new ConfigInstruction<GlobalConfig, string>(config => config.HasWebHookUrls = false, (config, value) => config.WebHookUrls = value) { Name = "WebHookUrls", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.WebHookUrlsV2, new ConfigInstruction<GlobalConfig, string>(config => config.HasWebHookUrlsV2 = false, (config, value) => config.WebHookUrlsV2 = value) { Name = "WebHookUrlsV2", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.WpfShowTitleAndArea, new ConfigInstruction<GlobalConfig, bool>(config => config.HasWpfShowTitleAndArea = false, (config, value) => config.WpfShowTitleAndArea = value) { Name = "WpfShowTitleAndArea", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.Cookie, new ConfigInstruction<GlobalConfig, string>(config => config.HasCookie = false, (config, value) => config.Cookie = value) { Name = "Cookie", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.LiveApiHost, new ConfigInstruction<GlobalConfig, string>(config => config.HasLiveApiHost = false, (config, value) => config.LiveApiHost = value) { Name = "LiveApiHost", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingCheckInterval, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingCheckInterval = false, (config, value) => config.TimingCheckInterval = value) { Name = "TimingCheckInterval", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingStreamRetry, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingStreamRetry = false, (config, value) => config.TimingStreamRetry = value) { Name = "TimingStreamRetry", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingStreamRetryNoQn, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingStreamRetryNoQn = false, (config, value) => config.TimingStreamRetryNoQn = value) { Name = "TimingStreamRetryNoQn", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingStreamConnect, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingStreamConnect = false, (config, value) => config.TimingStreamConnect = value) { Name = "TimingStreamConnect", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingDanmakuRetry, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingDanmakuRetry = false, (config, value) => config.TimingDanmakuRetry = value) { Name = "TimingDanmakuRetry", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.TimingWatchdogTimeout, new ConfigInstruction<GlobalConfig, uint>(config => config.HasTimingWatchdogTimeout = false, (config, value) => config.TimingWatchdogTimeout = value) { Name = "TimingWatchdogTimeout", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.RecordDanmakuFlushInterval, new ConfigInstruction<GlobalConfig, uint>(config => config.HasRecordDanmakuFlushInterval = false, (config, value) => config.RecordDanmakuFlushInterval = value) { Name = "RecordDanmakuFlushInterval", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.NetworkTransportUseSystemProxy, new ConfigInstruction<GlobalConfig, bool>(config => config.HasNetworkTransportUseSystemProxy = false, (config, value) => config.NetworkTransportUseSystemProxy = value) { Name = "NetworkTransportUseSystemProxy", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.NetworkTransportAllowedAddressFamily, new ConfigInstruction<GlobalConfig, AllowedAddressFamily>(config => config.HasNetworkTransportAllowedAddressFamily = false, (config, value) => config.NetworkTransportAllowedAddressFamily = value) { Name = "NetworkTransportAllowedAddressFamily", CanBeOptional = true });
            GlobalConfig.Add(GlobalConfigProperties.UserScript, new ConfigInstruction<GlobalConfig, string>(config => config.HasUserScript = false, (config, value) => config.UserScript = value) { Name = "UserScript", CanBeOptional = true });

            RoomConfig.Add(RoomConfigProperties.RoomId, new ConfigInstruction<RoomConfig, int>(config => config.HasRoomId = false, (config, value) => config.RoomId = value) { Name = "RoomId", CanBeOptional = false });
            RoomConfig.Add(RoomConfigProperties.AutoRecord, new ConfigInstruction<RoomConfig, bool>(config => config.HasAutoRecord = false, (config, value) => config.AutoRecord = value) { Name = "AutoRecord", CanBeOptional = false });
            RoomConfig.Add(RoomConfigProperties.RecordMode, new ConfigInstruction<RoomConfig, RecordMode>(config => config.HasRecordMode = false, (config, value) => config.RecordMode = value) { Name = "RecordMode", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.CuttingMode, new ConfigInstruction<RoomConfig, CuttingMode>(config => config.HasCuttingMode = false, (config, value) => config.CuttingMode = value) { Name = "CuttingMode", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.CuttingNumber, new ConfigInstruction<RoomConfig, uint>(config => config.HasCuttingNumber = false, (config, value) => config.CuttingNumber = value) { Name = "CuttingNumber", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordDanmaku, new ConfigInstruction<RoomConfig, bool>(config => config.HasRecordDanmaku = false, (config, value) => config.RecordDanmaku = value) { Name = "RecordDanmaku", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordDanmakuRaw, new ConfigInstruction<RoomConfig, bool>(config => config.HasRecordDanmakuRaw = false, (config, value) => config.RecordDanmakuRaw = value) { Name = "RecordDanmakuRaw", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordDanmakuSuperChat, new ConfigInstruction<RoomConfig, bool>(config => config.HasRecordDanmakuSuperChat = false, (config, value) => config.RecordDanmakuSuperChat = value) { Name = "RecordDanmakuSuperChat", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordDanmakuGift, new ConfigInstruction<RoomConfig, bool>(config => config.HasRecordDanmakuGift = false, (config, value) => config.RecordDanmakuGift = value) { Name = "RecordDanmakuGift", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordDanmakuGuard, new ConfigInstruction<RoomConfig, bool>(config => config.HasRecordDanmakuGuard = false, (config, value) => config.RecordDanmakuGuard = value) { Name = "RecordDanmakuGuard", CanBeOptional = true });
            RoomConfig.Add(RoomConfigProperties.RecordingQuality, new ConfigInstruction<RoomConfig, string>(config => config.HasRecordingQuality = false, (config, value) => config.RecordingQuality = value) { Name = "RecordingQuality", CanBeOptional = true });
        }
    }

}
