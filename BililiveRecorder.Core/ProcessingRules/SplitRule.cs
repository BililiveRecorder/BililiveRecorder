using System.Threading;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Core.ProcessingRules
{
    internal class SplitRule : ISimpleProcessingRule
    {
        // 0 = none, 1 = after, 2 = before
        private int splitFlag = 0;

        private const int FLAG_NONE = 0;
        private const int FLAG_BEFORE = 1;
        private const int FLAG_AFTER = 2;

        private static readonly ProcessingComment comment_before = new ProcessingComment(CommentType.Logging, "New file before data by split rule");
        private static readonly ProcessingComment comment_after = new ProcessingComment(CommentType.Logging, "New file after data by split rule");

        public void Run(FlvProcessingContext context, System.Action next)
        {
            var flag = Interlocked.Exchange(ref this.splitFlag, FLAG_NONE);

            if (FLAG_BEFORE == flag)
            {
                context.AddComment(comment_before);
                context.Actions.Insert(0, PipelineNewFileAction.Instance);
            }
            else if (FLAG_AFTER == flag)
            {
                context.AddComment(comment_after);
                context.Actions.Add(PipelineNewFileAction.Instance);
            }

            next();
        }

        public void SetSplitBeforeFlag() => Interlocked.Exchange(ref this.splitFlag, FLAG_BEFORE);

        public void SetSplitAfterFlag() => Interlocked.Exchange(ref this.splitFlag, FLAG_AFTER);
    }
}
