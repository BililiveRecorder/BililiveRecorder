using System.Collections.Generic;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Config
{
    [UsesVerify]
    public class ConfigTests
    {
        [Fact]
        public Task CanSerializeToJsonAsync()
        {
            var config = new ConfigV3()
            {
                Rooms = new List<RoomConfig>
                {
                    new RoomConfig { RoomId = 1, AutoRecord = true },
                    new RoomConfig { RoomId = 2 },
                    new RoomConfig { RoomId = int.MaxValue, RecordDanmaku = false }
                },
                Global = new GlobalConfig()
                {
                    FileNameRecordTemplate = "TEST TEMPLATE VALUE",
                    RecordDanmakuRaw = true,
                    RecordMode = RecordMode.RawData,
                }
            };

            var json = ConfigParser.SaveJson(config);

            return Verifier.Verify(json);
        }

        [Theory]
        [InlineData(@"{""version"":1,""data"":""{\""roomlist\"":[{\""enabled\"":true,\""id\"":1},{\""enabled\"":true,\""id\"":3},{\""enabled\"":true,\""id\"":5},{\""enabled\"":false,\""id\"":999},{\""enabled\"":false,\""id\"":2147483647}],\""record_filename_format\"":\""TEST_FILE_NAME_TEMPLATE!!\""}""}")]
        [InlineData(@"{""version"":2,""global"":{""RecordFilenameFormat"":{""HasValue"":true,""Value"":""TEST_FILE_NAME_TEMPLATE!!""}},""rooms"":[{""RoomId"":{""HasValue"":true,""Value"":1},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":3},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":5},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":999},""AutoRecord"":{""HasValue"":true,""Value"":false}},{""RoomId"":{""HasValue"":true,""Value"":2147483647},""AutoRecord"":{""HasValue"":true,""Value"":false}}]}")]
        [InlineData(@"{""version"":3,""global"":{""FileNameRecordTemplate"":{""HasValue"":true,""Value"":""TEST_FILE_NAME_TEMPLATE!!""}},""rooms"":[{""RoomId"":{""HasValue"":true,""Value"":1},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":3},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":5},""AutoRecord"":{""HasValue"":true,""Value"":true}},{""RoomId"":{""HasValue"":true,""Value"":999},""AutoRecord"":{""HasValue"":true,""Value"":false}},{""RoomId"":{""HasValue"":true,""Value"":2147483647},""AutoRecord"":{""HasValue"":true,""Value"":false}}]}")]
        public void CanLoadEveryVersion(string configString)
        {
            var config = ConfigParser.LoadJson(configString)!;

            Assert.NotNull(config);

            Assert.True(config.Global.HasFileNameRecordTemplate);
            Assert.Equal("TEST_FILE_NAME_TEMPLATE!!", config.Global.FileNameRecordTemplate);

            Assert.False(config.Global.HasRecordMode);

            Assert.Collection(config.Rooms,
                room =>
                {
                    Assert.True(room.AutoRecord);
                    Assert.Equal(1, room.RoomId);

                    Assert.False(room.HasRecordDanmaku);
                },
                room =>
                {
                    Assert.True(room.AutoRecord);
                    Assert.Equal(3, room.RoomId);

                    Assert.False(room.HasRecordDanmaku);
                },
                room =>
                {
                    Assert.True(room.AutoRecord);
                    Assert.Equal(5, room.RoomId);

                    Assert.False(room.HasRecordDanmaku);
                },
                room =>
                {
                    Assert.False(room.AutoRecord);
                    Assert.Equal(999, room.RoomId);

                    Assert.False(room.HasRecordDanmaku);
                },
                room =>
                {
                    Assert.False(room.AutoRecord);
                    Assert.Equal(int.MaxValue, room.RoomId);

                    Assert.False(room.HasRecordDanmaku);
                }
            );
        }
    }
}
