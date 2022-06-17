using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;

namespace BililiveRecorder.Flv.Writer
{
    public class FlvTagListWriter : IFlvTagWriter
    {
        private List<Tag>? file;

        public FlvTagListWriter()
        {
            this.Files = new List<List<Tag>>();
            this.AccompanyingTextLogs = new List<(double, string)>();
        }

        public List<List<Tag>> Files { get; }
        public List<(double lastTagDuration, string message)> AccompanyingTextLogs { get; }

        public long FileSize => -1;

        public object? State => null;

        public bool CloseCurrentFile()
        {
            if (this.file is null)
                return false;

            this.file = null;
            return true;
        }

        public Task CreateNewFile()
        {
            this.file = new List<Tag>();
            this.Files.Add(this.file);
            return Task.CompletedTask;
        }

        public void Dispose() { }

        public Task OverwriteMetadata(ScriptTagBody metadata) => Task.CompletedTask;

        public Task WriteAccompanyingTextLog(double lastTagDuration, string message)
        {
            this.AccompanyingTextLogs.Add((lastTagDuration, message));
            return Task.CompletedTask;
        }

        public Task WriteTag(Tag tag)
        {
            if (this.file is null)
                throw new InvalidOperationException();

            this.file.Add(tag);
            return Task.CompletedTask;
        }
    }
}
