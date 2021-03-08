using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理 end tag
    /// </summary>
    public class HandleEndTagRule : IFullProcessingRule
    {
        public Task RunAsync(FlvProcessingContext context, ProcessingDelegate next)
        {
            if (context.OriginalInput is PipelineEndAction)
                context.AddNewFileAtEnd();

            return next(context);
        }
    }
}
