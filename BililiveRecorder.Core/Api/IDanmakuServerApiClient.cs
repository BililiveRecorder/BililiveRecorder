using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;

namespace BililiveRecorder.Core.Api
{
    internal interface IDanmakuServerApiClient : IDisposable
    {
        long GetUid();

        Task<BilibiliApiResponse<DanmuInfo>> GetDanmakuServerAsync(int roomid);
    }
}
