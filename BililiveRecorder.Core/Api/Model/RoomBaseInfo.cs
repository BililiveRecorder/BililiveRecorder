using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api.Model
{
    /// <summary>
    /// 批量房间信息接口 getRoomBaseInfo 返回的单个房间条目。
    /// </summary>
    internal class RoomBaseInfo
    {
        [JsonProperty("uid")]
        public long Uid { get; set; }

        [JsonProperty("room_id")]
        public int RoomId { get; set; }

        [JsonProperty("short_id")]
        public int ShortId { get; set; }

        [JsonProperty("live_status")]
        public int LiveStatus { get; set; }

        [JsonProperty("parent_area_name")]
        public string ParentAreaName { get; set; } = string.Empty;

        [JsonProperty("area_name")]
        public string AreaName { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("uname")]
        public string Name { get; set; } = string.Empty;
    }
}
