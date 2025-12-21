using System;

namespace BililiveRecorder.Web.Models.Rest.Files
{
    public abstract class FileLikeDto
    {
        public abstract bool IsFolder { get; }

        public string Name { get; set; } = string.Empty;

        public DateTimeOffset LastModified { get; set; }
    }
}
