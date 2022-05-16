using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BililiveRecorder.Core.Api.Model
{
    internal class RoomInfo
    {
        [JsonProperty("room_info")]
        public InnerRoomInfo Room { get; set; } = new InnerRoomInfo();

        [JsonProperty("anchor_info")]
        public UserInfo User { get; set; } = new UserInfo();

        [JsonIgnore]
        public JObject? RawBilibiliApiJsonData;

        public class UserInfo
        {
            [JsonProperty("base_info")]
            public UserBaseInfo BaseInfo { get; set; } = null!;
        }

        public class UserBaseInfo
        {
            [JsonProperty("uname")]
            public string Name { get; set; } = string.Empty;
        }

        public class InnerRoomInfo
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
}
