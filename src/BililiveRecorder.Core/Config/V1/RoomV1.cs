using Newtonsoft.Json;

namespace BililiveRecorder.Core.Config.V1
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class RoomV1
    {
        [JsonProperty("id")]
        public int Roomid { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}
