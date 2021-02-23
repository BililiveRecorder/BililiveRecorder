using System;
using BililiveRecorder.Core.Api.Danmaku;

namespace BililiveRecorder.Core.Danmaku
{
    public interface IBasicDanmakuWriter : IDisposable
    {
        void Disable();
        void EnableWithPath(string path, IRoom room);
        void Write(DanmakuModel danmakuModel);
    }
}
