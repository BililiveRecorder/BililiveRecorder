using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BililiveRecorder.Web.Models.Rest.Logs
{
    public class JsonLogDto
    {
        public bool Continuous { get; set; }

        public long Cursor { get; set; }

        [JsonProperty(ItemConverterType = typeof(RawJsonStringConverter))]
        public IEnumerable<string> Logs { get; set; } = Array.Empty<string>();
    }
}
