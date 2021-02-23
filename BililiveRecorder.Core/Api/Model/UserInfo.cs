using Newtonsoft.Json;

namespace BililiveRecorder.Core.Api.Model
{
    public class UserInfo
    {
        [JsonProperty("info")]
        public InfoClass? Info { get; set; }

        public class InfoClass
        {
            [JsonProperty("uname")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
