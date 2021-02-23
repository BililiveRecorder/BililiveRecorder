using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Core.ProcessingRules
{
    public class SplitRule : IFullProcessingRule
    {
        private static readonly FlvProcessingContext NewFileContext = new FlvProcessingContext(PipelineNewFileAction.Instance, new Dictionary<object, object?>());

        // 0 = false, 1 = true
        private int splitFlag = 0;

        public async Task RunAsync(FlvProcessingContext context, ProcessingDelegate next)
        {
            await next(context).ConfigureAwait(false);

            if (1 == Interlocked.Exchange(ref this.splitFlag, 0))
            {
                await next(NewFileContext).ConfigureAwait(false);
                context.AddNewFileAtStart();
            }
        }

        public void SetSplitFlag() => Interlocked.Exchange(ref this.splitFlag, 1);
    }
}
