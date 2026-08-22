namespace BililiveRecorder.Core
{
    internal static class RoomLifecyclePolicy
    {
        internal static bool ShouldRefreshRoomInfo(bool autoRecord, bool roomInfoLoaded, bool recording) =>
            autoRecord || !roomInfoLoaded || recording;

        internal static bool ShouldCancelRecordTaskStartup(
            bool streaming,
            bool recordTaskExists,
            bool recordTaskReceiving) =>
            !streaming && recordTaskExists && !recordTaskReceiving;
    }
}
