namespace BililiveRecorder.Web.Models.Rest.Files
{
    public sealed class FileDto : FileLikeDto
    {
        /// <example>false</example>
        public override bool IsFolder => false;

        public long Size { get; set; }

        public string Url { get; set; } = string.Empty;
    }
}
