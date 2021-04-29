using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Danmaku;

namespace BililiveRecorder.Core.Danmaku
{
    public interface IBasicDanmakuWriter : IDisposable
    {
        void Disable();
        void EnableWithPath(string path, IRoom room);
        Task WriteAsync(DanmakuModel danmakuModel);
    }
}
