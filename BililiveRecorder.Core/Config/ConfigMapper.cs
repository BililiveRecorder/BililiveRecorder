using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BililiveRecorder.FlvProcessor;

#nullable enable
#pragma warning disable CS0612 // obsolete
namespace BililiveRecorder.Core.Config
{
    internal static class ConfigMapper
    {
        public static V2.ConfigV2 Map1To2(V1.ConfigV1 v1)
        {
            var map = new Dictionary<PropertyInfo, PropertyInfo>();

            AddMap<V1.ConfigV1, V2.GlobalConfig, EnabledFeature>(map, x => x.EnabledFeature, x => x.EnabledFeature);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.ClipLengthPast, x => x.ClipLengthPast);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.ClipLengthFuture, x => x.ClipLengthFuture);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.TimingStreamRetry, x => x.TimingStreamRetry);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.TimingStreamConnect, x => x.TimingStreamConnect);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.TimingDanmakuRetry, x => x.TimingDanmakuRetry);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.TimingCheckInterval, x => x.TimingCheckInterval);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.TimingWatchdogTimeout, x => x.TimingWatchdogTimeout);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.RecordDanmakuFlushInterval, x => x.RecordDanmakuFlushInterval);
            AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(map, x => x.Cookie, x => x.Cookie);
            AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(map, x => x.WebHookUrls, x => x.WebHookUrls);
            AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(map, x => x.LiveApiHost, x => x.LiveApiHost);
            AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(map, x => x.RecordFilenameFormat, x => x.RecordFilenameFormat);
            AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(map, x => x.ClipFilenameFormat, x => x.ClipFilenameFormat);

            AddMap<V1.ConfigV1, V2.GlobalConfig, AutoCuttingMode>(map, x => x.CuttingMode, x => x.CuttingMode);
            AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(map, x => x.CuttingNumber, x => x.CuttingNumber);
            AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(map, x => x.RecordDanmaku, x => x.RecordDanmaku);
            AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(map, x => x.RecordDanmakuRaw, x => x.RecordDanmakuRaw);
            AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(map, x => x.RecordDanmakuSuperChat, x => x.RecordDanmakuSuperChat);
            AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(map, x => x.RecordDanmakuGift, x => x.RecordDanmakuGift);
            AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(map, x => x.RecordDanmakuGuard, x => x.RecordDanmakuGuard);

            var def = new V1.ConfigV1(); // old default
            var v2 = new V2.ConfigV2();

            foreach (var item in map)
            {
                var data = item.Key.GetValue(v1);
                if (!(data?.Equals(item.Key.GetValue(def)) ?? true))
                    item.Value.SetValue(v2.Global, data);
            }

            v2.Rooms = v1.RoomList.Select(x => new V2.RoomConfig { RoomId = x.Roomid, AutoRecord = x.Enabled }).ToList();

            return v2;
        }

        private static void AddMap<T1, T2, T3>(Dictionary<PropertyInfo, PropertyInfo> map, Expression<Func<T1, T3>> keyExpr, Expression<Func<T2, T3>> valueExpr)
        {
            var key = GetProperty(keyExpr);
            var value = GetProperty(valueExpr);
            if ((key is null) || (value is null))
                return;
            map.Add(key, value);
        }

        private static PropertyInfo? GetProperty<TType, TValue>(Expression<Func<TType, TValue>> expression)
            => (expression.Body as MemberExpression)?.Member as PropertyInfo;
    }
}
