using System;
using System.Linq;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理收到音视频 Header 的情况
    /// </summary>
    /// <remarks>
    /// 当收到音视频 Header 时检查与上一组是否相同<br/>
    /// 并根据情况删除重复的 Header 或新建文件写入<br/>
    /// <br/>
    /// 本规则为一般规则
    /// </remarks>
    public class HandleNewHeaderRule : ISimpleProcessingRule
    {
        private const string VIDEO_HEADER_KEY = "HandleNewHeaderRule_VideoHeader";
        private const string AUDIO_HEADER_KEY = "HandleNewHeaderRule_AudioHeader";

        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is not PipelineHeaderAction header)
                return next();
            else
            {
                Tag? lastVideoHeader, lastAudioHeader;
                Tag? currentVideoHeader, currentAudioHeader;
                var multiple_header_present = false;

                // 从 Session Items 里取上次写入的 Header
                lastVideoHeader = context.SessionItems.ContainsKey(VIDEO_HEADER_KEY) ? context.SessionItems[VIDEO_HEADER_KEY] as Tag : null;
                lastAudioHeader = context.SessionItems.ContainsKey(AUDIO_HEADER_KEY) ? context.SessionItems[AUDIO_HEADER_KEY] as Tag : null;

                // 音频 视频 分别单独处理
                var group = header.AllTags.GroupBy(x => x.Type);

                { // 音频
                    var group_audio = group.FirstOrDefault(x => x.Key == TagType.Audio);
                    if (group_audio != null)
                    {
                        // 检查是否存在 **多个** **不同的** Header
                        if (group_audio.Count() > 1)
                        {
                            var first = group_audio.First();

                            if (group_audio.Skip(1).All(x => first.BinaryData?.SequenceEqual(x.BinaryData) ?? false))
                                currentAudioHeader = first;
                            else
                            {
                                // 默认最后一个为正确的
                                currentAudioHeader = group_audio.Last();
                                multiple_header_present = true;
                            }
                        }
                        else currentAudioHeader = group_audio.FirstOrDefault();
                    }
                    else currentAudioHeader = null;
                }

                { // 视频
                    var group_video = group.FirstOrDefault(x => x.Key == TagType.Video);
                    if (group_video != null)
                    {
                        // 检查是否存在 **多个** **不同的** Header
                        if (group_video.Count() > 1)
                        {
                            var first = group_video.First();

                            if (group_video.Skip(1).All(x => first.BinaryData?.SequenceEqual(x.BinaryData) ?? false))
                                currentVideoHeader = first;
                            else
                            {
                                // 默认最后一个为正确的
                                currentVideoHeader = group_video.Last();
                                multiple_header_present = true;
                            }
                        }
                        else currentVideoHeader = group_video.FirstOrDefault();
                    }
                    else currentVideoHeader = null;
                }

                if (multiple_header_present)
                    context.AddComment("收到了连续多个 Header");

                if (currentVideoHeader != null)
                    context.SessionItems[VIDEO_HEADER_KEY] = currentVideoHeader.Clone(); // TODO use memory provider
                if (currentAudioHeader != null)
                    context.SessionItems[AUDIO_HEADER_KEY] = currentAudioHeader.Clone();

                if (lastAudioHeader is null && lastVideoHeader is null)
                {
                    // 本 session 第一次输出 header
                    context.Output.Clear();

                    var output = new PipelineHeaderAction(Array.Empty<Tag>())
                    {
                        AudioHeader = currentAudioHeader?.Clone(),
                        VideoHeader = currentVideoHeader?.Clone(),
                    };

                    context.Output.Add(output);
                }
                else
                {
                    // 是否需要创建新文件
                    // 如果存在多个不同 Header 则必定创建新文件
                    var split_file = multiple_header_present;

                    // 如果最终选中的 Header 不等于上次写入的 Header
                    if (lastAudioHeader != null && !(currentAudioHeader?.BinaryData?.SequenceEqual(lastAudioHeader.BinaryData) ?? false))
                        split_file = true;
                    if (lastVideoHeader != null && !(currentVideoHeader?.BinaryData?.SequenceEqual(lastVideoHeader.BinaryData) ?? false))
                        split_file = true;

                    if (split_file)
                        context.AddComment("因为 Header 问题新建文件");

                    context.Output.Clear();

                    if (split_file)
                    {
                        context.AddNewFileAtStart();

                        var output = new PipelineHeaderAction(Array.Empty<Tag>())
                        {
                            AudioHeader = currentAudioHeader?.Clone(),
                            VideoHeader = currentVideoHeader?.Clone(),
                        };

                        context.Output.Add(output);

                        // 输出所有 Header 到一个独立的文件，以防出现无法播放的问题
                        // 如果不能正常播放，后期可以使用这里保存的 Header 配合 FlvInteractiveRebase 工具手动修复
                        if (multiple_header_present)
                            context.Output.Add(new PipelineLogAlternativeHeaderAction(header.AllTags));
                    }
                }
                return Task.CompletedTask;
            }
        }
    }
}
