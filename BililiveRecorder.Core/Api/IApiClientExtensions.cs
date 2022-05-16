using System;
using System.Linq;
using System.Threading.Tasks;
using static BililiveRecorder.Core.Api.Model.RoomPlayInfo;

namespace BililiveRecorder.Core.Api
{
    internal static class IApiClientExtensions
    {
        public static async Task<CodecItem?> GetCodecItemInStreamUrlAsync(this IApiClient apiClient, int roomid, int qn)
        {
            var apiResp = await apiClient.GetStreamUrlAsync(roomid: roomid, qn: qn).ConfigureAwait(false);
            var url_data = apiResp?.Data?.PlayurlInfo?.Playurl?.Streams;

            if (url_data is null)
                throw new Exception("playurl is null");

            var url_http_stream_flv_avc =
                url_data.FirstOrDefault(x => x.ProtocolName == "http_stream")
                ?.Formats?.FirstOrDefault(x => x.FormatName == "flv")
                ?.Codecs?.FirstOrDefault(x => x.CodecName == "avc");

            return url_http_stream_flv_avc;
        }
    }
}
