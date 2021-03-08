using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 检查分块内时间戳问题
    /// </summary>
    /// <remarks>
    /// 到目前为止还未发现有在一个 GOP 内出现时间戳异常问题<br/>
    /// 本规则是为了预防实际使用中遇到意外情况<br/>
    /// <br/>
    /// 本规则应该放在所有规则前面
    /// </remarks>
    public class CheckDiscontinuityRule : ISimpleProcessingRule
    {
        private const int MAX_ALLOWED_DIFF = 1000 * 10; // 10 seconds

        private static readonly ProcessingComment Comment1 = new ProcessingComment(CommentType.Unrepairable, "Flv Chunk 内出现时间戳跳变（变小）");
        private static readonly ProcessingComment Comment2 = new ProcessingComment(CommentType.Unrepairable, "Flv Chunk 内出现时间戳跳变（间隔过大）");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                for (var i = 0; i < data.Tags.Count - 1; i++)
                {
                    var f1 = data.Tags[i];
                    var f2 = data.Tags[i + 1];

                    if (f1.Timestamp > f2.Timestamp)
                    {
                        context.AddComment(Comment1);
                        yield return PipelineDisconnectAction.Instance;
                        yield return null;
                        yield break;
                    }
                    else if ((f2.Timestamp - f1.Timestamp) > MAX_ALLOWED_DIFF)
                    {
                        context.AddComment(Comment2);
                        yield return PipelineDisconnectAction.Instance;
                        yield return null;
                        yield break;
                    }
                }

                yield return data;
            }
            else
                yield return action;
        }
    }
}
