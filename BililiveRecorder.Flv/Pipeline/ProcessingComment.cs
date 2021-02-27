using System;

namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingComment
    {
        public ProcessingComment(CommentType commentType, string comment)
        {
            this.CommentType = commentType;
            this.Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }

        public ProcessingComment(CommentType commentType, string comment, bool skipCounting) : this(commentType, comment)
        {
            this.SkipCounting = skipCounting;
        }

        public CommentType CommentType { get; }
        public string Comment { get; }
        public bool SkipCounting { get; }

        public override string ToString() => $"{this.CommentType} {this.Comment}";
    }
}
