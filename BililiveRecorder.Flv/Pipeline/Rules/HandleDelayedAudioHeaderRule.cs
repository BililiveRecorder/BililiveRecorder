using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理延后收到的音频头
    /// </summary>
    /// <remarks>
    /// 本规则应该放在所有规则前面
    /// </remarks>
    public class HandleDelayedAudioHeaderRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.DecodingHeader, "检测到延后收到的音频头");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var tags = data.Tags;
                if (tags.Any(x => x.IsHeader()))
                {
                    context.AddComment(comment);

                    var index = tags.IndexOf(tags.Last(x => x.Flag == TagFlag.Header));
                    for (var i = 0; i < index; i++)
                    {
                        if (tags[i].Type == TagType.Audio)
                        {
                            // 在一段数据内 Header 之前出现了音频数据
                            yield return PipelineDisconnectAction.Instance;
                            yield return null;
                            yield break;
                        }
                    }

                    var headerTags = tags.Where(x => x.Flag == TagFlag.Header).ToList();
                    var newHeaderAction = new PipelineHeaderAction(headerTags);
                    var dataTags = tags.Where(x => x.Flag != TagFlag.Header).ToList();
                    var newDataAction = new PipelineDataAction(dataTags);

                    yield return newHeaderAction;
                    yield return newDataAction;
                }
                else
                    yield return data;
            }
            else
                yield return action;
        }
    }
}
