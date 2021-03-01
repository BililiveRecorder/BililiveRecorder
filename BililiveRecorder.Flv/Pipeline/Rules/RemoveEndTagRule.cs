using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 移除 end tag
    /// </summary>
    public class RemoveEndTagRule : ISimpleProcessingRule
    {
        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is PipelineEndAction)
            {
                context.ClearOutput();
                return Task.CompletedTask;
            }
            else
                return next();
        }
    }
}
