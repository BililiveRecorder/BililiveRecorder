using System;

namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingComment
    {
        public ProcessingComment(CommentType type, bool actionRequired, string comment)
        {
            this.Type = type;
            this.ActionRequired = actionRequired;
            this.Comment = comment ?? throw new ArgumentNullException(nameof(comment));
        }

        /// <summary>
        /// Type
        /// </summary>
        public CommentType Type { get; }

        /// <summary>
        /// Action Required
        /// </summary>
        public bool ActionRequired { get; }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; }

        public override string ToString() => $"({this.Type},{(this.ActionRequired ? "A" : "C")}): {this.Comment}";
    }
}
