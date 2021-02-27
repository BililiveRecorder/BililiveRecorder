using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Core.ProcessingRules
{
    public class SplitRule : IFullProcessingRule
    {
        private static readonly FlvProcessingContext NewFileContext =
            new FlvProcessingContext(PipelineNewFileAction.Instance, new Dictionary<object, object?>());

        // 0 = none, 1 = after, 2 = before
        private int splitFlag = 0;

        private const int FLAG_NONE = 0;
        private const int FLAG_BEFORE = 1;
        private const int FLAG_AFTER = 2;

        public async Task RunAsync(FlvProcessingContext context, ProcessingDelegate next)
        {
            var flag = Interlocked.Exchange(ref this.splitFlag, FLAG_NONE);

            if (FLAG_BEFORE == flag)
            {
                await next(NewFileContext).ConfigureAwait(false);
                context.AddNewFileAtStart();
            }

            await next(context).ConfigureAwait(false);

            if (FLAG_AFTER == flag)
            {
                await next(NewFileContext).ConfigureAwait(false);
                context.AddNewFileAtEnd();
            }
        }

        public void SetSplitBeforeFlag() => Interlocked.Exchange(ref this.splitFlag, FLAG_BEFORE);

        public void SetSplitAfterFlag() => Interlocked.Exchange(ref this.splitFlag, FLAG_AFTER);
    }
}
