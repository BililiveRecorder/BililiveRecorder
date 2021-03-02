using System;
using System.Linq;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 修复 Tag 错位等时间戳相关问题
    /// </summary>
    public class UpdateDataTagOrderRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment1 = new ProcessingComment(CommentType.Unrepairable, "GOP内音频或视频时间戳变小");
        private static readonly ProcessingComment comment2 = new ProcessingComment(CommentType.Unrepairable, "GOP内音频时间戳相比视频时间戳过小");

        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is not PipelineDataAction data)
                return next();

            // 如果一切正常，直接跳过
            if (!data.Tags.Any2((t1, t2) => t1.Timestamp > t2.Timestamp))
                return next();

            // 如果音频和视频单独判断还有问题，则判定为无法修复
            if (data.Tags.Where(x => x.Type == TagType.Audio).Any2((a, b) => a.Timestamp > b.Timestamp)
                || data.Tags.Where(x => x.Type == TagType.Video).Any2((a, b) => a.Timestamp > b.Timestamp))
            {
                context.ClearOutput();
                context.AddDisconnectAtStart();
                context.AddComment(comment1);
                return Task.CompletedTask;
            }

            var audio1 = data.Tags.First(x => x.Type == TagType.Audio);
            var video1 = data.Tags.First(x => x.Type == TagType.Video);
            var diff = audio1.Timestamp - video1.Timestamp;

            if (diff >= 0)
            {
                // 正常
            }
            else if (diff >= -5)
            {
                // 音频时间戳比视频早，把音频整体向后偏移
                context.AddComment(new ProcessingComment(CommentType.TimestampJump, "GOP内音频时间戳轻度偏移"));

                foreach (var tag in data.Tags.Where(x => x.Type == TagType.Audio))
                    tag.Timestamp -= diff;
            }
            else
            {
                // 音频时间戳比视频早太多，判定为无法修复
                context.ClearOutput();
                context.AddDisconnectAtStart();
                context.AddComment(comment2);
                return Task.CompletedTask;
            }

            // 排序
            data.Tags = data.Tags.OrderBy(x => x.Timestamp).ToList();

            return next();
        }
    }
}
