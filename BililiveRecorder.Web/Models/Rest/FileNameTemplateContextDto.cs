namespace BililiveRecorder.Web.Models.Rest
{
    public class FileNameTemplateContextDto
    {
        public int RoomId { get; set; }
        public int ShortId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string AreaParent { get; set; } = string.Empty;
        public string AreaChild { get; set; } = string.Empty;
        public int Qn { get; set; }

        /// <example>{}</example>
        public string? Json { get; set; }
    }
}
