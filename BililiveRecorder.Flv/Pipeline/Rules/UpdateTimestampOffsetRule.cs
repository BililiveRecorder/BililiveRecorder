using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateTimestampOffsetRule : ISimpleProcessingRule
    {
        private const int MAX_ALLOWED_DIFF = 1000 * 10; // 10 seconds

        private static readonly ProcessingComment comment1 = new ProcessingComment(CommentType.Unrepairable, "GOP 内音频或视频时间戳不连续");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private bool CheckIfNormal(IEnumerable<Tag> data) => !data.Any2((a, b) => a.Timestamp > b.Timestamp);

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var isNormal = this.CheckIfNormal(data.Tags);
                if (isNormal)
                {
                    yield return data;
                    yield break;
                }

                // 这个问题可能不能稳定修复，如果是在录直播最好还是断开重连，获取正常的直播流
                // TODO 确认修复效果
                yield return PipelineDisconnectAction.Instance;

                if (!(this.CheckIfNormal(data.Tags.Where(x => x.Type == TagType.Audio)) && this.CheckIfNormal(data.Tags.Where(x => x.Type == TagType.Video))))
                {
                    context.AddComment(comment1);
                    yield break;
                }
                else
                {
                    var audio = data.Tags.First(x => x.Type == TagType.Audio);
                    var video = data.Tags.First(x => x.Type == TagType.Video);

                    var diff = audio.Timestamp - video.Timestamp;

                    if (diff > 50)
                    {
                        context.AddComment(new ProcessingComment(CommentType.TimestampOffset, $"音视频时间戳偏移, A: {audio.Timestamp}, V: {video.Timestamp}, D: {diff}"));
                        foreach (var tag in data.Tags.Where(x => x.Type == TagType.Audio))
                        {
                            tag.Timestamp -= diff;
                        }
                    }

                    // 因为上面已经检查了音频或视频单独不存在时间戳跳变问题，所以可以进行排序
                    data.Tags = data.Tags.OrderBy(x => x.Timestamp).ToList();

                    yield return data;
                }
            }
            else
                yield return action;
        }
    }
}
