using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api.Model
{
    public class RoomInfo
    {
        [JsonProperty("room_id")]
        public int RoomId { get; set; }

        [JsonProperty("short_id")]
        public int ShortId { get; set; }

        [JsonProperty("live_status")]
        public int LiveStatus { get; set; }

        [JsonProperty("area_id")]
        public int AreaId { get; set; }

        [JsonProperty("parent_area_id")]
        public int ParentAreaId { get; set; }

        [JsonProperty("area_name")]
        public string AreaName { get; set; } = string.Empty;

        [JsonProperty("parent_area_name")]
        public string ParentAreaName { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
    }
}
