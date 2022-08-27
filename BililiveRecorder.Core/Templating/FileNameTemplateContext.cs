using Newtonsoft.Json.Linq;

namespace BililiveRecorder.Core.Templating
{
    public class FileNameTemplateContext
    {
        public int RoomId { get; set; }

        public int ShortId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AreaParent { get; set; } = string.Empty;

        public string AreaChild { get; set; } = string.Empty;

        public int PartIndex { get; set; }

        public int Qn { get; set; }

        public JObject? Json { get; set; }
    }
}
