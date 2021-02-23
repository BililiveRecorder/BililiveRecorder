using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V1;
using BililiveRecorder.Core.Config.V2;
using Newtonsoft.Json;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Config
{
    public class ConfigTests
    {
        private const string V2TestString1 = "{\"version\":2,\"global\":{\"EnabledFeature\":{\"HasValue\":false,\"Value\":0},\"ClipLengthPast\":{\"HasValue\":false,\"Value\":0},\"ClipLengthFuture\":{\"HasValue\":false,\"Value\":0},\"TimingStreamRetry\":{\"HasValue\":false,\"Value\":0},\"TimingStreamConnect\":{\"HasValue\":false,\"Value\":0},\"TimingDanmakuRetry\":{\"HasValue\":false,\"Value\":0},\"TimingCheckInterval\":{\"HasValue\":false,\"Value\":0},\"TimingWatchdogTimeout\":{\"HasValue\":false,\"Value\":0},\"RecordDanmakuFlushInterval\":{\"HasValue\":false,\"Value\":0},\"Cookie\":{\"HasValue\":false,\"Value\":null},\"WebHookUrls\":{\"HasValue\":false,\"Value\":null},\"LiveApiHost\":{\"HasValue\":false,\"Value\":null},\"RecordFilenameFormat\":{\"HasValue\":false,\"Value\":null},\"ClipFilenameFormat\":{\"HasValue\":false,\"Value\":null},\"CuttingMode\":{\"HasValue\":false,\"Value\":0},\"CuttingNumber\":{\"HasValue\":false,\"Value\":0},\"RecordDanmaku\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuRaw\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuSuperChat\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGift\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGuard\":{\"HasValue\":false,\"Value\":false}},\"rooms\":[{\"RoomId\":{\"HasValue\":true,\"Value\":1},\"AutoRecord\":{\"HasValue\":false,\"Value\":false},\"CuttingMode\":{\"HasValue\":false,\"Value\":0},\"CuttingNumber\":{\"HasValue\":false,\"Value\":0},\"RecordDanmaku\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuRaw\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuSuperChat\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGift\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGuard\":{\"HasValue\":false,\"Value\":false}},{\"RoomId\":{\"HasValue\":true,\"Value\":2},\"AutoRecord\":{\"HasValue\":false,\"Value\":false},\"CuttingMode\":{\"HasValue\":false,\"Value\":0},\"CuttingNumber\":{\"HasValue\":false,\"Value\":0},\"RecordDanmaku\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuRaw\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuSuperChat\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGift\":{\"HasValue\":false,\"Value\":false},\"RecordDanmakuGuard\":{\"HasValue\":false,\"Value\":false}}]}";

        private static readonly List<object[]> V1 = new()
        {
            new object[] { "{\"version\":1}", 1 },
            new object[] { "{\"version\":1,\"data\":\"\"}", 1 },
        };

        private static readonly List<object[]> V2 = new()
        {
            new object[] { "{\"version\":2}", 2 },
            new object[] { "{\"version\":2,\"data\":{}}", 2 },
            new object[] { V2TestString1, 2 },
        };

        public static IEnumerable<object[]> GetTestData(int version)
            => version switch
            {
                0 => V1.Concat(V2).AsEnumerable(),
                1 => V1.AsEnumerable(),
                2 => V2.AsEnumerable(),
                _ => throw new ArgumentException()
            };

        [Theory, MemberData(nameof(GetTestData), 0)]
        public void Parse(string data, int ver)
        {
            var result = JsonConvert.DeserializeObject<ConfigBase>(data);

            var type = ver switch
            {
                1 => typeof(ConfigV1Wrapper),
                2 => typeof(ConfigV2),
                _ => throw new Exception("not supported")
            };

            Assert.Equal(type, result.GetType());
        }

        [Fact]
        public void V2Test1()
        {
            var obj = JsonConvert.DeserializeObject<ConfigBase>(V2TestString1);

            var v2 = Assert.IsType<ConfigV2>(obj);
            Assert.Equal(2, v2.Rooms.Count);
            Assert.Equal(1, v2.Rooms[0].RoomId);
            Assert.Equal(2, v2.Rooms[1].RoomId);
        }

        [Fact]
        public void Save()
        {
            ConfigBase config = new ConfigV2()
            {
                Rooms = new List<RoomConfig>
                {
                    new RoomConfig { RoomId = 1 },
                    new RoomConfig { RoomId = 2 }
                },
                Global = new GlobalConfig()
            };

            var json = JsonConvert.SerializeObject(config);
        }
    }
}
