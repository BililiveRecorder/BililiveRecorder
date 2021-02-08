using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理收到 Script Tag 的情况
    /// </summary>
    /// <remarks>
    /// 本规则为一般规则
    /// </remarks>
    public class HandleNewScriptRule : ISimpleProcessingRule
    {
        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is PipelineScriptAction)
            {
                context.AddNewFileAtStart();
                return Task.CompletedTask;
            }
            else return next();
        }
    }
}
