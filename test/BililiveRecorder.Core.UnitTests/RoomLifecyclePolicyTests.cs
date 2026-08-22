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
        [InlineData(false, true, true)]
        [InlineData(false, false, false)]
        [InlineData(true, true, false)]
        public void ShouldStopOnlyAnActiveRecordingWhenRoomIsOffline(
            bool streaming,
            bool recording,
            bool expected)
        {
            Assert.Equal(
                expected,
                RoomLifecyclePolicy.ShouldStopRecordingWhenOffline(streaming, recording));
        }
    }
}
