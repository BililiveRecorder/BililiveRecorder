using System;

namespace BililiveRecorder.Core
{
    public interface IDanmakuReceiver
    {
        bool IsConnected { get; }
        Exception Error { get; }
        uint ViewerCount { get; }

        event DisconnectEvt Disconnected;
        event ReceivedRoomCountEvt ReceivedRoomCount;
        event ReceivedDanmakuEvt ReceivedDanmaku;

        bool Connect(int roomId);
        void Disconnect();
    }
}
