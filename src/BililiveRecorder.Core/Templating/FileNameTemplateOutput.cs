using System;

namespace BililiveRecorder.Core.Templating
{
    public readonly struct FileNameTemplateOutput
    {
        public FileNameTemplateOutput(FileNameTemplateStatus status, string? errorMessage, string relativePath, string? fullPath)
        {
            this.Status = status;
            this.ErrorMessage = errorMessage;
            this.RelativePath = relativePath ?? throw new ArgumentNullException(nameof(relativePath));
            this.FullPath = fullPath;
        }

        public FileNameTemplateStatus Status { get; }

        public string? ErrorMessage { get; }

        public string RelativePath { get; }

        public string? FullPath { get; }
    }
}
