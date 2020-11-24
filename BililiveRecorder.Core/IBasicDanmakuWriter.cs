using System;

namespace BililiveRecorder.Core
{
    public interface IBasicDanmakuWriter : IDisposable
    {
        void Disable();
        void EnableWithPath(string path);
        void Write(DanmakuModel danmakuModel);
    }
}
