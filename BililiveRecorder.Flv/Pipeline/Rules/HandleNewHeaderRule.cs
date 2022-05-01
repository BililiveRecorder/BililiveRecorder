using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;
using StructLinq.Where;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理音视频 Header。收到音视频 Header 时检查与上一组是否相同，并根据情况删除重复的 Header 或新建文件。<br/>
    /// </summary>
    public class HandleNewHeaderRule : ISimpleProcessingRule
    {
        private const string VIDEO_HEADER_KEY = "HandleNewHeaderRule_VideoHeader";
        private const string AUDIO_HEADER_KEY = "HandleNewHeaderRule_AudioHeader";

        private static readonly ProcessingComment MultipleHeaderComment = new ProcessingComment(CommentType.DecodingHeader, "收到了连续多个 Header，新建文件");
        private static readonly ProcessingComment SplitFileComment = new ProcessingComment(CommentType.DecodingHeader, "因为 Header 问题新建文件");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineHeaderAction header)
            {
                // 从 Session Items 里取上次写入的 Header
                var lastVideoHeader = context.SessionItems.ContainsKey(VIDEO_HEADER_KEY) ? context.SessionItems[VIDEO_HEADER_KEY] as Tag : null;
                var lastAudioHeader = context.SessionItems.ContainsKey(AUDIO_HEADER_KEY) ? context.SessionItems[AUDIO_HEADER_KEY] as Tag : null;

                var multiple_header_present = false;

                // 音频 视频 分别单独处理
                var group = header.AllTags.GroupBy(x => x.Type);

                var currentVideoHeader = SelectHeader(ref multiple_header_present, header.AllTags.ToStructEnumerable().Where(ref LinqFunctions.TagIsVideo, x => x));
                var currentAudioHeader = SelectHeader(ref multiple_header_present, header.AllTags.ToStructEnumerable().Where(ref LinqFunctions.TagIsAudio, x => x));

                if (multiple_header_present)
                    context.AddComment(MultipleHeaderComment);

                // 是否需要创建新文件
                // 如果存在多个不同 Header 则必定创建新文件
                var split_file = multiple_header_present;

                DecideSplit(ref lastVideoHeader, ref currentVideoHeader, ref split_file);
                DecideSplit(ref lastAudioHeader, ref currentAudioHeader, ref split_file);

                if (currentVideoHeader != null)
                    context.SessionItems[VIDEO_HEADER_KEY] = currentVideoHeader.Clone(); // TODO use memory provider
                if (currentAudioHeader != null)
                    context.SessionItems[AUDIO_HEADER_KEY] = currentAudioHeader.Clone();

                // 如果最终选中的 Header 不等于上次写入的 Header
                //if (currentAudioHeader is not null && lastAudioHeader is not null && !(currentAudioHeader.BinaryData?.SequenceEqual(lastAudioHeader.BinaryData) ?? false))
                //    split_file = true;
                //if (currentVideoHeader is not null && lastVideoHeader is not null && !(currentVideoHeader.BinaryData?.SequenceEqual(lastVideoHeader.BinaryData) ?? false))
                //    split_file = true;

                var notFirstTime = lastAudioHeader is not null || lastVideoHeader is not null;

                if (notFirstTime // 第一次触发规则不判定为有问题
                    && split_file
                    && !multiple_header_present)
                    context.AddComment(SplitFileComment);

                if (notFirstTime && split_file)
                    yield return PipelineNewFileAction.Instance;

                if (split_file)
                    yield return new PipelineHeaderAction(Array.Empty<Tag>())
                    {
                        AudioHeader = currentAudioHeader?.Clone(),
                        VideoHeader = currentVideoHeader?.Clone(),
                    };

                // 输出所有 Header 到一个独立的文件，以防出现无法播放的问题
                // 如果不能正常播放，后期可以使用这里保存的 Header 配合 FlvInteractiveRebase 工具手动修复
                if (multiple_header_present)
                    yield return new PipelineLogAlternativeHeaderAction(header.AllTags);

            }
            else
                yield return action;
        }

        private static Tag? SelectHeader<TEnumerable, TEnumerator, TFunction>(ref bool multiple_header_present, WhereEnumerable<Tag, TEnumerable, TEnumerator, TFunction> tags)
            where TEnumerable : struct, IStructEnumerable<Tag, TEnumerator>
            where TEnumerator : struct, IStructEnumerator<Tag>
            where TFunction : struct, IFunction<Tag, bool>
        {
            Tag? currentHeader;

            if (tags.Count(x => x) > 1)
            {
                // 检查是否存在 **多个** **不同的** Header
                var first = tags.First(x => x);

                if (tags.Skip(1, x => x).All(x => first.BinaryData?.SequenceEqual(x.BinaryData) ?? false))
                    currentHeader = first;
                else
                {
                    // 默认最后一个为正确的
                    currentHeader = tags.Last(x => x);
                    multiple_header_present = true;
                }
            }
            else
                currentHeader = tags.FirstOrDefault(x => x);

            return currentHeader;
        }

        private static void DecideSplit(ref Tag? lastHeader, ref Tag? currentHeader, ref bool split_file)
        {
            if (lastHeader is null)
            {
                if (currentHeader is null)
                {
                    // 从未出现过、并且本次还没收到
                    // 忽略不动
                }
                else
                {
                    // 之前未出现过 header
                    // 所以以新收到的为准并切割文件

                    // currentHeader = currentHeader;
                    split_file = true;
                }
            }
            else
            {
                if (currentHeader is null)
                {
                    // 以前收到过 header 但是本次没收到
                    // 说明是收到了另一种 header
                    // 使用上次收到的 header
                    currentHeader = lastHeader;
                }
                else
                {
                    // 之前收到过、这次也收到了
                    // 对 header 内容进行对比

                    if (currentHeader.BinaryData?.SequenceEqual(lastHeader.BinaryData) ?? false) // 如果 BinaryData 为 null 则判定为不相同
                    {
                        // 如果内容相同、则忽略
                        // currentHeader = currentHeader;
                    }
                    else
                    {
                        // 如果内容不同，则使用新收到的 header 并切分文件
                        // currentHeader = currentHeader;
                        split_file = true;
                    }
                }
            }
        }
    }
}
