// ******************************
//  GENERATED CODE, DO NOT EDIT MANUALLY.
//  SEE /config_gen/README.md
// ******************************

using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using GraphQL.Types;
using HierarchicalPropertyDefault;
#nullable enable
namespace BililiveRecorder.Web.Models
{
    public class SetRoomConfig
    {
        public bool? AutoRecord { get; set; }
        public Optional<RecordMode>? OptionalRecordMode { get; set; }
        public Optional<CuttingMode>? OptionalCuttingMode { get; set; }
        public Optional<uint>? OptionalCuttingNumber { get; set; }
        public Optional<bool>? OptionalRecordDanmaku { get; set; }
        public Optional<bool>? OptionalRecordDanmakuRaw { get; set; }
        public Optional<bool>? OptionalRecordDanmakuSuperChat { get; set; }
        public Optional<bool>? OptionalRecordDanmakuGift { get; set; }
        public Optional<bool>? OptionalRecordDanmakuGuard { get; set; }
        public Optional<string?>? OptionalRecordingQuality { get; set; }

        public void ApplyTo(RoomConfig config)
        {
            if (this.AutoRecord.HasValue) config.AutoRecord = this.AutoRecord.Value;
            if (this.OptionalRecordMode.HasValue) config.OptionalRecordMode = this.OptionalRecordMode.Value;
            if (this.OptionalCuttingMode.HasValue) config.OptionalCuttingMode = this.OptionalCuttingMode.Value;
            if (this.OptionalCuttingNumber.HasValue) config.OptionalCuttingNumber = this.OptionalCuttingNumber.Value;
            if (this.OptionalRecordDanmaku.HasValue) config.OptionalRecordDanmaku = this.OptionalRecordDanmaku.Value;
            if (this.OptionalRecordDanmakuRaw.HasValue) config.OptionalRecordDanmakuRaw = this.OptionalRecordDanmakuRaw.Value;
            if (this.OptionalRecordDanmakuSuperChat.HasValue) config.OptionalRecordDanmakuSuperChat = this.OptionalRecordDanmakuSuperChat.Value;
            if (this.OptionalRecordDanmakuGift.HasValue) config.OptionalRecordDanmakuGift = this.OptionalRecordDanmakuGift.Value;
            if (this.OptionalRecordDanmakuGuard.HasValue) config.OptionalRecordDanmakuGuard = this.OptionalRecordDanmakuGuard.Value;
            if (this.OptionalRecordingQuality.HasValue) config.OptionalRecordingQuality = this.OptionalRecordingQuality.Value;
        }
    }

    public class SetGlobalConfig
    {
        public Optional<RecordMode>? OptionalRecordMode { get; set; }
        public Optional<CuttingMode>? OptionalCuttingMode { get; set; }
        public Optional<uint>? OptionalCuttingNumber { get; set; }
        public Optional<bool>? OptionalRecordDanmaku { get; set; }
        public Optional<bool>? OptionalRecordDanmakuRaw { get; set; }
        public Optional<bool>? OptionalRecordDanmakuSuperChat { get; set; }
        public Optional<bool>? OptionalRecordDanmakuGift { get; set; }
        public Optional<bool>? OptionalRecordDanmakuGuard { get; set; }
        public Optional<string?>? OptionalRecordingQuality { get; set; }
        public Optional<string?>? OptionalFileNameRecordTemplate { get; set; }
        public Optional<string?>? OptionalWebHookUrls { get; set; }
        public Optional<string?>? OptionalWebHookUrlsV2 { get; set; }
        public Optional<bool>? OptionalWpfShowTitleAndArea { get; set; }
        public Optional<string?>? OptionalCookie { get; set; }
        public Optional<string?>? OptionalLiveApiHost { get; set; }
        public Optional<uint>? OptionalTimingCheckInterval { get; set; }
        public Optional<uint>? OptionalTimingStreamRetry { get; set; }
        public Optional<uint>? OptionalTimingStreamRetryNoQn { get; set; }
        public Optional<uint>? OptionalTimingStreamConnect { get; set; }
        public Optional<uint>? OptionalTimingDanmakuRetry { get; set; }
        public Optional<uint>? OptionalTimingWatchdogTimeout { get; set; }
        public Optional<uint>? OptionalRecordDanmakuFlushInterval { get; set; }
        public Optional<bool>? OptionalNetworkTransportUseSystemProxy { get; set; }
        public Optional<AllowedAddressFamily>? OptionalNetworkTransportAllowedAddressFamily { get; set; }
        public Optional<string?>? OptionalUserScript { get; set; }

        public void ApplyTo(GlobalConfig config)
        {
            if (this.OptionalRecordMode.HasValue) config.OptionalRecordMode = this.OptionalRecordMode.Value;
            if (this.OptionalCuttingMode.HasValue) config.OptionalCuttingMode = this.OptionalCuttingMode.Value;
            if (this.OptionalCuttingNumber.HasValue) config.OptionalCuttingNumber = this.OptionalCuttingNumber.Value;
            if (this.OptionalRecordDanmaku.HasValue) config.OptionalRecordDanmaku = this.OptionalRecordDanmaku.Value;
            if (this.OptionalRecordDanmakuRaw.HasValue) config.OptionalRecordDanmakuRaw = this.OptionalRecordDanmakuRaw.Value;
            if (this.OptionalRecordDanmakuSuperChat.HasValue) config.OptionalRecordDanmakuSuperChat = this.OptionalRecordDanmakuSuperChat.Value;
            if (this.OptionalRecordDanmakuGift.HasValue) config.OptionalRecordDanmakuGift = this.OptionalRecordDanmakuGift.Value;
            if (this.OptionalRecordDanmakuGuard.HasValue) config.OptionalRecordDanmakuGuard = this.OptionalRecordDanmakuGuard.Value;
            if (this.OptionalRecordingQuality.HasValue) config.OptionalRecordingQuality = this.OptionalRecordingQuality.Value;
            if (this.OptionalFileNameRecordTemplate.HasValue) config.OptionalFileNameRecordTemplate = this.OptionalFileNameRecordTemplate.Value;
            if (this.OptionalWebHookUrls.HasValue) config.OptionalWebHookUrls = this.OptionalWebHookUrls.Value;
            if (this.OptionalWebHookUrlsV2.HasValue) config.OptionalWebHookUrlsV2 = this.OptionalWebHookUrlsV2.Value;
            if (this.OptionalWpfShowTitleAndArea.HasValue) config.OptionalWpfShowTitleAndArea = this.OptionalWpfShowTitleAndArea.Value;
            if (this.OptionalCookie.HasValue) config.OptionalCookie = this.OptionalCookie.Value;
            if (this.OptionalLiveApiHost.HasValue) config.OptionalLiveApiHost = this.OptionalLiveApiHost.Value;
            if (this.OptionalTimingCheckInterval.HasValue) config.OptionalTimingCheckInterval = this.OptionalTimingCheckInterval.Value;
            if (this.OptionalTimingStreamRetry.HasValue) config.OptionalTimingStreamRetry = this.OptionalTimingStreamRetry.Value;
            if (this.OptionalTimingStreamRetryNoQn.HasValue) config.OptionalTimingStreamRetryNoQn = this.OptionalTimingStreamRetryNoQn.Value;
            if (this.OptionalTimingStreamConnect.HasValue) config.OptionalTimingStreamConnect = this.OptionalTimingStreamConnect.Value;
            if (this.OptionalTimingDanmakuRetry.HasValue) config.OptionalTimingDanmakuRetry = this.OptionalTimingDanmakuRetry.Value;
            if (this.OptionalTimingWatchdogTimeout.HasValue) config.OptionalTimingWatchdogTimeout = this.OptionalTimingWatchdogTimeout.Value;
            if (this.OptionalRecordDanmakuFlushInterval.HasValue) config.OptionalRecordDanmakuFlushInterval = this.OptionalRecordDanmakuFlushInterval.Value;
            if (this.OptionalNetworkTransportUseSystemProxy.HasValue) config.OptionalNetworkTransportUseSystemProxy = this.OptionalNetworkTransportUseSystemProxy.Value;
            if (this.OptionalNetworkTransportAllowedAddressFamily.HasValue) config.OptionalNetworkTransportAllowedAddressFamily = this.OptionalNetworkTransportAllowedAddressFamily.Value;
            if (this.OptionalUserScript.HasValue) config.OptionalUserScript = this.OptionalUserScript.Value;
        }
    }

}

namespace BililiveRecorder.Web.Models.Rest
{
    public class RoomConfigDto
    {
        public bool AutoRecord { get; set; }
        public Optional<RecordMode> OptionalRecordMode { get; set; }
        public Optional<CuttingMode> OptionalCuttingMode { get; set; }
        public Optional<uint> OptionalCuttingNumber { get; set; }
        public Optional<bool> OptionalRecordDanmaku { get; set; }
        public Optional<bool> OptionalRecordDanmakuRaw { get; set; }
        public Optional<bool> OptionalRecordDanmakuSuperChat { get; set; }
        public Optional<bool> OptionalRecordDanmakuGift { get; set; }
        public Optional<bool> OptionalRecordDanmakuGuard { get; set; }
        public Optional<string?> OptionalRecordingQuality { get; set; }
    }

    public class GlobalConfigDto
    {
        public Optional<RecordMode> OptionalRecordMode { get; set; }
        public Optional<CuttingMode> OptionalCuttingMode { get; set; }
        public Optional<uint> OptionalCuttingNumber { get; set; }
        public Optional<bool> OptionalRecordDanmaku { get; set; }
        public Optional<bool> OptionalRecordDanmakuRaw { get; set; }
        public Optional<bool> OptionalRecordDanmakuSuperChat { get; set; }
        public Optional<bool> OptionalRecordDanmakuGift { get; set; }
        public Optional<bool> OptionalRecordDanmakuGuard { get; set; }
        public Optional<string?> OptionalRecordingQuality { get; set; }
        public Optional<string?> OptionalFileNameRecordTemplate { get; set; }
        public Optional<string?> OptionalWebHookUrls { get; set; }
        public Optional<string?> OptionalWebHookUrlsV2 { get; set; }
        public Optional<bool> OptionalWpfShowTitleAndArea { get; set; }
        public Optional<string?> OptionalCookie { get; set; }
        public Optional<string?> OptionalLiveApiHost { get; set; }
        public Optional<uint> OptionalTimingCheckInterval { get; set; }
        public Optional<uint> OptionalTimingStreamRetry { get; set; }
        public Optional<uint> OptionalTimingStreamRetryNoQn { get; set; }
        public Optional<uint> OptionalTimingStreamConnect { get; set; }
        public Optional<uint> OptionalTimingDanmakuRetry { get; set; }
        public Optional<uint> OptionalTimingWatchdogTimeout { get; set; }
        public Optional<uint> OptionalRecordDanmakuFlushInterval { get; set; }
        public Optional<bool> OptionalNetworkTransportUseSystemProxy { get; set; }
        public Optional<AllowedAddressFamily> OptionalNetworkTransportAllowedAddressFamily { get; set; }
        public Optional<string?> OptionalUserScript { get; set; }
    }

}

namespace BililiveRecorder.Web.Models.Graphql
{
    internal class RoomConfigType : ObjectGraphType<RoomConfig>
    {
        public RoomConfigType()
        {
            this.Field(x => x.RoomId);
            this.Field(x => x.AutoRecord);
            this.Field(x => x.OptionalRecordMode, type: typeof(HierarchicalOptionalType<RecordMode>));
            this.Field(x => x.OptionalCuttingMode, type: typeof(HierarchicalOptionalType<CuttingMode>));
            this.Field(x => x.OptionalCuttingNumber, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalRecordDanmaku, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuRaw, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuSuperChat, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGift, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGuard, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordingQuality, type: typeof(HierarchicalOptionalType<string>));
        }
    }

    internal class GlobalConfigType : ObjectGraphType<GlobalConfig>
    {
        public GlobalConfigType()
        {
            this.Field(x => x.OptionalRecordMode, type: typeof(HierarchicalOptionalType<RecordMode>));
            this.Field(x => x.OptionalCuttingMode, type: typeof(HierarchicalOptionalType<CuttingMode>));
            this.Field(x => x.OptionalCuttingNumber, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalRecordDanmaku, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuRaw, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuSuperChat, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGift, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGuard, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalRecordingQuality, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalFileNameRecordTemplate, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalWebHookUrls, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalWebHookUrlsV2, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalWpfShowTitleAndArea, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalCookie, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalLiveApiHost, type: typeof(HierarchicalOptionalType<string>));
            this.Field(x => x.OptionalTimingCheckInterval, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalTimingStreamRetry, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalTimingStreamRetryNoQn, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalTimingStreamConnect, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalTimingDanmakuRetry, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalTimingWatchdogTimeout, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalRecordDanmakuFlushInterval, type: typeof(HierarchicalOptionalType<uint>));
            this.Field(x => x.OptionalNetworkTransportUseSystemProxy, type: typeof(HierarchicalOptionalType<bool>));
            this.Field(x => x.OptionalNetworkTransportAllowedAddressFamily, type: typeof(HierarchicalOptionalType<AllowedAddressFamily>));
            this.Field(x => x.OptionalUserScript, type: typeof(HierarchicalOptionalType<string>));
        }
    }

    internal class DefaultConfigType : ObjectGraphType<DefaultConfig>
    {
        public DefaultConfigType()
        {
            this.Field(x => x.RecordMode);
            this.Field(x => x.CuttingMode);
            this.Field(x => x.CuttingNumber);
            this.Field(x => x.RecordDanmaku);
            this.Field(x => x.RecordDanmakuRaw);
            this.Field(x => x.RecordDanmakuSuperChat);
            this.Field(x => x.RecordDanmakuGift);
            this.Field(x => x.RecordDanmakuGuard);
            this.Field(x => x.RecordingQuality);
            this.Field(x => x.FileNameRecordTemplate);
            this.Field(x => x.WebHookUrls);
            this.Field(x => x.WebHookUrlsV2);
            this.Field(x => x.WpfShowTitleAndArea);
            this.Field(x => x.Cookie);
            this.Field(x => x.LiveApiHost);
            this.Field(x => x.TimingCheckInterval);
            this.Field(x => x.TimingStreamRetry);
            this.Field(x => x.TimingStreamRetryNoQn);
            this.Field(x => x.TimingStreamConnect);
            this.Field(x => x.TimingDanmakuRetry);
            this.Field(x => x.TimingWatchdogTimeout);
            this.Field(x => x.RecordDanmakuFlushInterval);
            this.Field(x => x.NetworkTransportUseSystemProxy);
            this.Field(x => x.NetworkTransportAllowedAddressFamily);
            this.Field(x => x.UserScript);
        }
    }

    internal class SetRoomConfigType : InputObjectGraphType<SetRoomConfig>
    {
        public SetRoomConfigType()
        {
            this.Field(x => x.AutoRecord, nullable: true);
            this.Field(x => x.OptionalRecordMode, nullable: true, type: typeof(HierarchicalOptionalInputType<RecordMode>));
            this.Field(x => x.OptionalCuttingMode, nullable: true, type: typeof(HierarchicalOptionalInputType<CuttingMode>));
            this.Field(x => x.OptionalCuttingNumber, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalRecordDanmaku, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuRaw, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuSuperChat, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGift, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGuard, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordingQuality, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
        }
    }

    internal class SetGlobalConfigType : InputObjectGraphType<SetGlobalConfig>
    {
        public SetGlobalConfigType()
        {
            this.Field(x => x.OptionalRecordMode, nullable: true, type: typeof(HierarchicalOptionalInputType<RecordMode>));
            this.Field(x => x.OptionalCuttingMode, nullable: true, type: typeof(HierarchicalOptionalInputType<CuttingMode>));
            this.Field(x => x.OptionalCuttingNumber, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalRecordDanmaku, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuRaw, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuSuperChat, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGift, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordDanmakuGuard, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalRecordingQuality, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalFileNameRecordTemplate, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalWebHookUrls, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalWebHookUrlsV2, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalWpfShowTitleAndArea, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalCookie, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalLiveApiHost, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
            this.Field(x => x.OptionalTimingCheckInterval, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalTimingStreamRetry, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalTimingStreamRetryNoQn, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalTimingStreamConnect, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalTimingDanmakuRetry, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalTimingWatchdogTimeout, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalRecordDanmakuFlushInterval, nullable: true, type: typeof(HierarchicalOptionalInputType<uint>));
            this.Field(x => x.OptionalNetworkTransportUseSystemProxy, nullable: true, type: typeof(HierarchicalOptionalInputType<bool>));
            this.Field(x => x.OptionalNetworkTransportAllowedAddressFamily, nullable: true, type: typeof(HierarchicalOptionalInputType<AllowedAddressFamily>));
            this.Field(x => x.OptionalUserScript, nullable: true, type: typeof(HierarchicalOptionalInputType<string>));
        }
    }

}
