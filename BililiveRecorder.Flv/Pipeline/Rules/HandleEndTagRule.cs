using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理 end tag
    /// </summary>
    public class HandleEndTagRule : ISimpleProcessingRule
    {
        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            yield return action;
            if (action is PipelineEndAction)
                yield return PipelineNewFileAction.Instance;
        }
    }
}
