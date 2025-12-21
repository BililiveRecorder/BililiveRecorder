using System;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;

namespace BililiveRecorder.Core.Api
{
    internal interface IApiClient : IDisposable
    {
        Task<BilibiliApiResponse<RoomInfo>> GetRoomInfoAsync(int roomid);
        Task<BilibiliApiResponse<RoomPlayInfo>> GetStreamUrlAsync(int roomid, int qn);
    }
}
