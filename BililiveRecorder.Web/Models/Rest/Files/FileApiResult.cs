using System;
using System.Collections.Generic;

namespace BililiveRecorder.Web.Models.Rest.Files
{
    public sealed class FileApiResult
    {
        public static readonly FileApiResult NotExist = new FileApiResult(false, string.Empty, Array.Empty<FileLikeDto>());

        public FileApiResult(bool exist, string path, IReadOnlyList<FileLikeDto> files)
        {
            this.Exist = exist;
            this.Path = path ?? throw new ArgumentNullException(nameof(path));
            this.Files = files ?? throw new ArgumentNullException(nameof(files));
        }

        public bool Exist { get; }

        public string Path { get; }

        public IReadOnlyList<FileLikeDto> Files { get; }
    }
}
