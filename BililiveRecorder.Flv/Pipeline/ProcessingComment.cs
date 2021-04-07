using System;

namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingComment
    {
        public ProcessingComment(CommentType t, string c)
        {
            this.T = t;
            this.C = c ?? throw new ArgumentNullException(nameof(c));
        }

        /// <summary>
        /// Type
        /// </summary>
        public CommentType T { get; }

        /// <summary>
        /// Comment
        /// </summary>
        public string C { get; }

        public override string ToString() => $"{this.T} {this.C}";
    }
}
