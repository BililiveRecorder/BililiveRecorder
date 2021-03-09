using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 修复 Tag 错位等时间戳相关问题
    /// </summary>
    public class UpdateDataTagOrderRule : ISimpleProcessingRule
    {
        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is not PipelineDataAction data)
            {
                yield return action;
                yield break;
            }

            // 如果一切正常，直接跳过
            if (data.Tags.Any2((t1, t2) => t1.Timestamp > t2.Timestamp))
            {
                // 排序
                data.Tags = data.Tags.OrderBy(x => x.Timestamp).ToList();
            }

            yield return data;
        }
    }
}
