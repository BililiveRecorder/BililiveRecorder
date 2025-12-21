using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理延后收到的音频头，移动到音视频数据的前面。
    /// </summary>
    public class HandleDelayedAudioHeaderRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment1 = new ProcessingComment(CommentType.Unrepairable, true, "音频数据出现在音频头之前");
        private static readonly ProcessingComment comment2 = new ProcessingComment(CommentType.DecodingHeader, true, "检测到延后收到的音频头");

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
                // 如果分组内含有 Heaer
                if (tags.ToStructEnumerable().Any(ref LinqFunctions.TagIsHeader, x => x))
                {
                    {
                        var shouldReportError = false;
                        for (var i = tags.Count - 1; i >= 0; i--)
                        {
                            if (tags[i].Type == TagType.Audio)
                            {
                                if (tags[i].Flag != TagFlag.None)
                                {
                                    // 发现了 Audio Header
                                    shouldReportError = true;
                                }
                                else
                                {
                                    // 在一段数据内 Header 之前出现了音频数据
                                    if (shouldReportError)
                                    {
                                        context.AddComment(comment1);
                                        yield return new PipelineDisconnectAction("直播音频数据中间出现音频头");
                                        yield return PipelineNewFileAction.Instance;
                                        yield return null;
                                        yield break;
                                    }
                                }
                            }
                        }
                    }

                    context.AddComment(comment2);

                    var headerTags = tags.ToStructEnumerable().Where(ref LinqFunctions.TagIsHeader, x => x).ToArray();
                    var newHeaderAction = new PipelineHeaderAction(headerTags);

                    var dataTags = tags.ToStructEnumerable().Except(headerTags.ToStructEnumerable(), x => x, x => x).ToArray();
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
