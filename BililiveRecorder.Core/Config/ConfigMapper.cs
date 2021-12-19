using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#pragma warning disable CS0612 // obsolete
#pragma warning disable CS0618 // Type or member is obsolete
namespace BililiveRecorder.Core.Config
{
    internal static class ConfigMapper
    {
        private static readonly Dictionary<PropertyInfo, PropertyInfo> Map1to2 = new();
        private static readonly Dictionary<PropertyInfo, PropertyInfo> Map2to3GlobalConfig = new();
        private static readonly Dictionary<PropertyInfo, PropertyInfo> Map2to3RoomConfig = new();

        static ConfigMapper()
        {
            // Map v1 to v2
            {
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.TimingStreamRetry, x => x.TimingStreamRetry);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.TimingStreamConnect, x => x.TimingStreamConnect);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.TimingDanmakuRetry, x => x.TimingDanmakuRetry);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.TimingCheckInterval, x => x.TimingCheckInterval);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.TimingWatchdogTimeout, x => x.TimingWatchdogTimeout);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.RecordDanmakuFlushInterval, x => x.RecordDanmakuFlushInterval);
                AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(Map1to2, x => x.Cookie, x => x.Cookie);
                AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(Map1to2, x => x.WebHookUrls, x => x.WebHookUrls);
                AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(Map1to2, x => x.LiveApiHost, x => x.LiveApiHost);
                AddMap<V1.ConfigV1, V2.GlobalConfig, string?>(Map1to2, x => x.RecordFilenameFormat, x => x.RecordFilenameFormat);

                AddMap<V1.ConfigV1, V2.GlobalConfig, CuttingMode>(Map1to2, x => x.CuttingMode, x => x.CuttingMode);
                AddMap<V1.ConfigV1, V2.GlobalConfig, uint>(Map1to2, x => x.CuttingNumber, x => x.CuttingNumber);
                AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(Map1to2, x => x.RecordDanmaku, x => x.RecordDanmaku);
                AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(Map1to2, x => x.RecordDanmakuRaw, x => x.RecordDanmakuRaw);
                AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(Map1to2, x => x.RecordDanmakuSuperChat, x => x.RecordDanmakuSuperChat);
                AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(Map1to2, x => x.RecordDanmakuGift, x => x.RecordDanmakuGift);
                AddMap<V1.ConfigV1, V2.GlobalConfig, bool>(Map1to2, x => x.RecordDanmakuGuard, x => x.RecordDanmakuGuard);
            }

            // Map v2 to v3
            {
                Map2to3GlobalConfig = GetPropertyInfoPairs<V2.GlobalConfig, V3.GlobalConfig>()
                    .Where(x => x.Key.Name.StartsWith("Optional", StringComparison.Ordinal))
                    .ToDictionary(x => x.Key, x => x.Value);

                Map2to3RoomConfig = GetPropertyInfoPairs<V2.RoomConfig, V3.RoomConfig>()
                    .Where(x => x.Key.Name.StartsWith("Optional", StringComparison.Ordinal))
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public static V2.ConfigV2 Map1To2(V1.ConfigV1 v1)
        {
            var def = new V1.ConfigV1(); // old default
            var v2 = new V2.ConfigV2();

            foreach (var item in Map1to2)
            {
                var data = item.Key.GetValue(v1);
                if (!(data?.Equals(item.Key.GetValue(def)) ?? true))
                    item.Value.SetValue(v2.Global, data);
            }

            v2.Rooms = v1.RoomList.Select(x => new V2.RoomConfig { RoomId = x.Roomid, AutoRecord = x.Enabled }).ToList();

            return v2;
        }

        public static V3.ConfigV3 Map2To3(V2.ConfigV2 v2)
        {
            var v3 = new V3.ConfigV3();

            // 复制没有变动的房间独立设置
            foreach (var v2room in v2.Rooms)
            {
                var v3room = new V3.RoomConfig();
                CopyValueWithMap(Map2to3RoomConfig, v2room, v3room);
                v3.Rooms.Add(v3room);
            }

            // 复制没有变动的全局设置
            CopyValueWithMap(Map2to3GlobalConfig, v2.Global, v3.Global);

            // 转换文件名格式
            // 如果用户设置了自定义的文件名格式才需要转换，否则使用全局默认
            if (v2.Global.HasRecordFilenameFormat && v2.Global.RecordFilenameFormat is not null)
            {
                v3.Global.FileNameRecordTemplate = v2.Global.RecordFilenameFormat
                    .Replace("{date}", "{{ \"now\" | format_date: \"yyyyMMdd\" }}")
                    .Replace("{time}", "{{ \"now\" | format_date: \"HHmmss\" }}")
                    .Replace("{ms}", "{{ \"now\" | format_date: \"fff\" }}")
                    .Replace("{random}", "{% random 3 %}")
                    .Replace("{roomid}", "{{ roomId }}")
                    .Replace("{title}", "{{ title }}")
                    .Replace("{name}", "{{ name }}")
                    .Replace("{parea}", "{{ areaParent }}")
                    .Replace("{area}", "{{ areaChild }}")
                    ;
            }

            return v3;
        }

        private static void CopyValueWithMap(Dictionary<PropertyInfo, PropertyInfo> map, object source, object target)
        {
            foreach (var item in map)
            {
                var data = item.Key.GetValue(source);
                item.Value.SetValue(target, data);
            }
        }

        private static List<KeyValuePair<PropertyInfo, PropertyInfo>> GetPropertyInfoPairs<T1, T2>()
            where T1 : class
            where T2 : class
        {
            var result = new List<KeyValuePair<PropertyInfo, PropertyInfo>>(32);

            var t1Type = typeof(T1);
            var t2Type = typeof(T2);

            var t1 = t1Type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (var p1 in t1)
            {
                var p2 = t2Type.GetProperty(p1.Name, BindingFlags.Instance | BindingFlags.Public);
                if (p2 != null)
                    result.Add(new KeyValuePair<PropertyInfo, PropertyInfo>(p1, p2));
            }

            return result;
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
