using BililiveRecorder.Core;
using Xunit;

namespace BililiveRecorder.Core.UnitTests
{
    public class RoomLifecyclePolicyTests
    {
        [Theory]
        [InlineData(true, true, false, true)]
        [InlineData(false, false, false, true)]
        [InlineData(false, true, true, true)]
        [InlineData(false, true, false, false)]
        public void ShouldRefreshRoomInfoWhenLifecycleNeedsStatus(
            bool autoRecord,
            bool roomInfoLoaded,
            bool recording,
            bool expected)
        {
            Assert.Equal(
                expected,
                RoomLifecyclePolicy.ShouldRefreshRoomInfo(autoRecord, roomInfoLoaded, recording));
        }

        [Theory]
        [InlineData(false, true, false, true)]
        [InlineData(false, true, true, false)]
        [InlineData(false, false, false, false)]
        [InlineData(true, true, false, false)]
        public void ShouldCancelOnlyARecordTaskThatHasNotStartedWhenRoomIsOffline(
            bool streaming,
            bool recordTaskExists,
            bool recordTaskReceiving,
            bool expected)
        {
            Assert.Equal(
                expected,
                RoomLifecyclePolicy.ShouldCancelRecordTaskStartup(
                    streaming,
                    recordTaskExists,
                    recordTaskReceiving));
        }
    }
}
