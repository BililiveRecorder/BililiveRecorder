using BililiveRecorder.Core.Api.Danmaku;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Danmaku
{
    public class DanmakuModelTests
    {
        [Fact]
        public void DanmakuModel_Should_Handle_DANMU_MSG_MIRROR()
        {
            // Arrange - JSON for a cross-room danmaku message
            var json = @"{
                ""cmd"": ""DANMU_MSG_MIRROR"",
                ""info"": [
                    [0, 1, 25, 16777215, 1234567890, 0, 0, ""abc"", 0, 0, 0, """", 0, """", """", {""extra"": {""is_mirror"": true}}],
                    ""Test cross-room danmaku"",
                    [123456, ""TestUser"", ""0"", ""0"", 0, 10000, 1, """"],
                    [],
                    [],
                    [],
                    0,
                    0
                ]
            }";

            // Act
            var model = new DanmakuModel(json);

            // Assert
            Assert.Equal(DanmakuMsgType.Comment, model.MsgType);
            Assert.Equal("Test cross-room danmaku", model.CommentText);
            Assert.Equal(123456L, model.UserID);
            Assert.Equal("TestUser", model.UserName);
        }

        [Fact]
        public void DanmakuModel_Should_Handle_Regular_DANMU_MSG()
        {
            // Arrange - JSON for a regular danmaku message
            var json = @"{
                ""cmd"": ""DANMU_MSG"",
                ""info"": [
                    [0, 1, 25, 16777215, 1234567890, 0, 0, ""abc"", 0, 0, 0],
                    ""Test regular danmaku"",
                    [654321, ""RegularUser"", ""0"", ""0"", 0, 10000, 1, """"],
                    [],
                    [],
                    [],
                    0,
                    0
                ]
            }";

            // Act
            var model = new DanmakuModel(json);

            // Assert
            Assert.Equal(DanmakuMsgType.Comment, model.MsgType);
            Assert.Equal("Test regular danmaku", model.CommentText);
            Assert.Equal(654321L, model.UserID);
            Assert.Equal("RegularUser", model.UserName);
        }
    }
}
