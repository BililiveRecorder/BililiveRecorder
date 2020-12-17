using System;

namespace BililiveRecorder.Core
{
    public interface IBasicDanmakuWriter : IDisposable
    {
        void Disable();
        void EnableWithPath(string path, IRecordedRoom recordedRoom);
        void Write(DanmakuModel danmakuModel);
    }
}
