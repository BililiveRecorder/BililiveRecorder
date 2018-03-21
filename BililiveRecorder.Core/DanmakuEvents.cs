using System;

namespace BililiveRecorder.Core
{
    public delegate void DisconnectEvt(object sender, DisconnectEvtArgs e);
    public class DisconnectEvtArgs
    {
        public Exception Error;
    }

    public delegate void ReceivedRoomCountEvt(object sender, ReceivedRoomCountArgs e);
    public class ReceivedRoomCountArgs
    {
        public uint UserCount;
    }

    public delegate void ReceivedDanmakuEvt(object sender, ReceivedDanmakuArgs e);
    public class ReceivedDanmakuArgs
    {
        public DanmakuModel Danmaku;
    }
}
