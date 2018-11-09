using Newtonsoft.Json;

namespace BililiveRecorder.Core.Config
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class RoomV1
    {
        [JsonProperty("id")]
        public int Roomid { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}
