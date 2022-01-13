using System;
using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class UpdateCompositionTimeRule : ISimpleProcessingRule
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

            var videoTags = data.Tags.Where(x => x.Type == TagType.Video);

            if (!videoTags.Any())
            {
                // skip
                yield return data;
                yield break;
            }

            if (videoTags.Any(x => x.ExtraData is null))
            {
                context.AddComment(new ProcessingComment(CommentType.Unrepairable, "有 Tag 的 ExtraData 为 null，请检查文件或联系开发者"));
                yield break;
            }

            var compositionOffset = videoTags.Min(x => x.ExtraData!.CompositionTime);

            if (compositionOffset is <= 0 or >= int.MaxValue)
            {
                // skip
                yield return data;
                yield break;
            }
            else
            {
                foreach (var tag in data.Tags)
                {
                    if (tag.Type != TagType.Video)
                        continue;

                    System.Diagnostics.Debug.WriteLine("CompositionOffset: " + compositionOffset);
                    tag.ExtraData!.CompositionTime -= compositionOffset;
                    tag.Timestamp += compositionOffset;
                }
                yield return data;
                yield break;
            }
        }
    }
}
