using System;
using System.Threading.Tasks;

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

        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is PipelineDataAction data)
            {
                for (var i = 0; i < data.Tags.Count - 1; i++)
                {
                    var f1 = data.Tags[i];
                    var f2 = data.Tags[i + 1];

                    if (f1.Timestamp > f2.Timestamp)
                    {
                        context.ClearOutput();
                        context.AddDisconnectAtStart();
                        context.AddComment("Flv Chunk 内出现时间戳跳变（变小）");
                        return Task.CompletedTask;
                    }
                    else if ((f2.Timestamp - f1.Timestamp) > MAX_ALLOWED_DIFF)
                    {
                        context.ClearOutput();
                        context.AddDisconnectAtStart();
                        context.AddComment("Flv Chunk 内出现时间戳跳变（间隔过大）");
                        return Task.CompletedTask;
                    }
                }
                return next();
            }
            else return next();
        }
    }
}
