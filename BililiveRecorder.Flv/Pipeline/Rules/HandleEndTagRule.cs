using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理 end tag
    /// </summary>
    public class HandleEndTagRule : ISimpleProcessingRule
    {
        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is PipelineEndAction end)
            {
                if (context.SessionItems.TryGetValue(UpdateTimestampRule.TS_STORE_KEY, out var obj) && obj is UpdateTimestampRule.TimestampStore store)
                    end.Tag.Timestamp -= store.CurrentOffset;
                else
                    end.Tag.Timestamp = 0;

                context.AddNewFileAtEnd();
                return Task.CompletedTask;
            }
            else
                return next();
        }
    }
}
