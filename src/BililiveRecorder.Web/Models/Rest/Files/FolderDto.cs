namespace BililiveRecorder.Web.Models.Rest.Files
{
    public sealed class FolderDto : FileLikeDto
    {
        /// <example>true</example>
        public override bool IsFolder => true;
    }
}
