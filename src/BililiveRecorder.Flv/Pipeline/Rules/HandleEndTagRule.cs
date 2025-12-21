using System;
using System.Collections.Generic;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理 End Tag，遇到的时候对文件进行分段
    /// </summary>
    public class HandleEndTagRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.Logging, false, "因收到 End Tag 分段");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            yield return action;
            if (action is PipelineEndAction)
            {
                context.AddComment(comment);
                yield return PipelineNewFileAction.Instance;
            }
        }
    }
}
