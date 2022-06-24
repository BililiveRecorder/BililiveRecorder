using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Rules;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BililiveRecorder.Flv.Tests.RuleTests
{
    public abstract class IntegratedTestBase
    {
        protected static async Task RunPipeline(ITagGroupReader reader, IFlvTagWriter output, List<ProcessingComment> comments)
        {
            var writer = new FlvProcessingContextWriter(tagWriter: output, allowMissingHeader: true, disableKeyframes: true, logger: null);
            var session = new Dictionary<object, object?>();
            var context = new FlvProcessingContext();
            var pipeline = new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).Add<FfmpegDetectionRule>().AddDefault().AddRemoveFillerData().Build();

            while (true)
            {
                var group = await reader.ReadGroupAsync(default).ConfigureAwait(false);

                if (group is null)
                    break;

                context.Reset(group, session);
                pipeline(context);

                comments.AddRange(context.Comments);
                await writer.WriteAsync(context).ConfigureAwait(false);
            }
        }

        protected static async Task AssertTagsByRerunPipeline(List<Tag> tags)
        {
            var reader = new TagGroupReader(new FlvTagListReader(tags.Select(x => x.Clone()).ToList()));
            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            await RunPipeline(reader, output, comments).ConfigureAwait(false);

            // 忽略 ignore Logging 
            comments.RemoveAll(x => !x.ActionRequired);
            // 不应该有任何问题 Shouldn't have any problems
            Assert.Empty(comments);
            // 不应该有多个 Header Shouldn't have multiple headers
            Assert.Empty(output.AccompanyingTextLogs);

            // 只应该有一个文件输出 Should output only a single file
            var outputTags = Assert.Single(output.Files);
            // Tag count should match
            Assert.Equal(tags.Count, outputTags.Count);

            for (var i = 0; i < tags.Count; i++)
            {
                var a = tags[i];
                var b = outputTags[i];

                Assert.NotSame(a, b);
                Assert.Equal(a.Type, b.Type);
                Assert.Equal(a.Flag, b.Flag);
                Assert.Equal(a.Index, b.Index);
                Assert.Equal(a.Size, b.Size);
                Assert.Equal(a.Timestamp, b.Timestamp);
                Assert.Equal(a.BinaryDataForSerializationUseOnly, b.BinaryDataForSerializationUseOnly);
            }
        }
    }
}
