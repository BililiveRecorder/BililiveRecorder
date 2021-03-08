using System.Linq;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理延后收到的音频头
    /// </summary>
    /// <remarks>
    /// 本规则应该放在所有规则前面
    /// </remarks>
    public class HandleDelayedAudioHeaderRule : IFullProcessingRule
    {
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.DecodingHeader, "检测到延后收到的音频头");

        public Task RunAsync(FlvProcessingContext context, ProcessingDelegate next)
        {
            if (context.OriginalInput is PipelineDataAction dataAction)
            {
                if (!dataAction.Tags.Any(x => x.IsHeader()))
                    return next(context);
                else
                    return this.RunAsyncCore(dataAction, context, next);
            }
            else
                return next(context);
        }

        private async Task RunAsyncCore(PipelineDataAction dataAction, FlvProcessingContext context, ProcessingDelegate next)
        {
            context.ClearOutput();
            context.AddComment(comment);

            var tags = dataAction.Tags;
            var index = tags.IndexOf(tags.Last(x => x.Flag == TagFlag.Header));
            for (var i = 0; i < index; i++)
            {
                if (tags[i].Type == TagType.Audio)
                {
                    context.AddDisconnectAtStart();
                    return;
                }
            }

            var headerTags = tags.Where(x => x.Flag == TagFlag.Header).ToList();
            var newHeaderAction = new PipelineHeaderAction(headerTags);
            var dataTags = tags.Where(x => x.Flag != TagFlag.Header).ToList();
            var newDataAction = new PipelineDataAction(dataTags);

            var localContext = new FlvProcessingContext(newHeaderAction, context.SessionItems);

            await next(localContext).ConfigureAwait(false);
            context.Output.AddRange(localContext.Output);
            context.Comments.AddRange(localContext.Comments);

            localContext.Reset(newDataAction, context.SessionItems);

            await next(localContext).ConfigureAwait(false);
            context.Output.AddRange(localContext.Output);
            context.Comments.AddRange(localContext.Comments);

            // TODO fix me
            //var oi = context.Output.IndexOf(dataAction);
            //context.Output.Insert(oi,newHeaderAction);
        }
    }
}
