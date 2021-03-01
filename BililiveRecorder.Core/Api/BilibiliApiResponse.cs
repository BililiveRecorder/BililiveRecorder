using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api
{
    public class BilibiliApiResponse<T> where T : class
    {
        [JsonProperty("code")]
        public int? Code { get; set; }

        [JsonProperty("data")]
        public T? Data { get; set; }
    }
}
