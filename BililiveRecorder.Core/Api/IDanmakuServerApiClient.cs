using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;

namespace BililiveRecorder.Core.Api
{
    public interface IDanmakuServerApiClient : IDisposable
    {
        Task<BilibiliApiResponse<DanmuInfo>> GetDanmakuServerAsync(int roomid);
    }
}
