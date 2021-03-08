using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 检查缺少关键帧的问题
    /// </summary>
    /// <remarks>
    /// 到目前为止还未发现有出现过此问题<br/>
    /// 本规则是为了预防实际使用中遇到意外情况<br/>
    /// <br/>
    /// 本规则应该放在所有规则前面
    /// </remarks>
    public class CheckMissingKeyframeRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.Unrepairable, "Flv Chunk 内缺少关键帧");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var f = data.Tags.FirstOrDefault(x => x.Type == TagType.Video);
                if (f == null || (0 == (f.Flag & TagFlag.Keyframe)))
                {
                    context.AddComment(comment);
                    yield return PipelineDisconnectAction.Instance;
                    yield return null;
                    yield break;
                }
                else
                    yield return action;
            }
            else
                yield return action;
        }
    }
}
