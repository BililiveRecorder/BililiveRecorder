using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public abstract class TestBase
    {
        protected XmlFlvFile LoadFile(string path)
        {
            Stream? stream = null;
            try
            {
                var gz_path = path + ".gz";
                if (Path.GetExtension(path) == ".gz")
                    stream = new GZipStream(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
                else if (File.Exists(gz_path))
                    stream = new GZipStream(File.Open(gz_path, FileMode.Open, FileAccess.Read, FileShare.Read), CompressionMode.Decompress);
                else
                    stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                return (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(stream);
            }
            finally
            {
                stream?.Dispose();
            }
        }

        protected ProcessingDelegate BuildPipeline() =>
            new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).AddDefault().AddRemoveFillerData().Build();

        protected async Task RunPipeline(ITagGroupReader reader, IFlvTagWriter output, List<ProcessingComment> comments)
        {
            var writer = new FlvProcessingContextWriter(tagWriter: output, allowMissingHeader: true, disableKeyframes: true);
            var session = new Dictionary<object, object?>();
            var context = new FlvProcessingContext();
            var pipeline = this.BuildPipeline();

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

        protected async Task AssertTagsByRerunPipeline(List<Tag> tags)
        {
            var reader = new TagGroupReader(new FlvTagListReader(tags.Select(x => x.Clone()).ToList()));
            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            await this.RunPipeline(reader, output, comments).ConfigureAwait(false);

            comments.RemoveAll(x => x.T == CommentType.Logging);
            Assert.Empty(comments);
            Assert.Empty(output.AlternativeHeaders);

            var file = Assert.Single(output.Files);
            Assert.Equal(tags.Count, file.Count);
            for (var i = 0; i < tags.Count; i++)
            {
                var a = tags[i];
                var b = file[i];

                Assert.NotSame(a, b);
                Assert.Equal(a.Type, b.Type);
                Assert.Equal(a.Flag, b.Flag);
                Assert.Equal(a.Index, b.Index);
                Assert.Equal(a.Size, b.Size);
                Assert.Equal(a.Timestamp, b.Timestamp);
                Assert.Equal(a.BinaryDataForSerializationUseOnly, b.BinaryDataForSerializationUseOnly);
            }
        }

        protected void AssertTagsShouldPassBasicChecks(List<Tag> tags)
        {
            Assert.True(tags.Any2((a, b) => (a.Timestamp <= b.Timestamp) && (b.Timestamp - a.Timestamp < 50)));

            Assert.Equal(TagType.Script, tags[0].Type);
            Assert.Equal(0, tags[0].Timestamp);

            Assert.Equal(TagType.Video, tags[1].Type);
            Assert.Equal(0, tags[1].Timestamp);
            Assert.Equal(TagFlag.Header | TagFlag.Keyframe, tags[1].Flag);

            Assert.Equal(TagType.Audio, tags[2].Type);
            Assert.Equal(0, tags[2].Timestamp);
            Assert.Equal(TagFlag.Header, tags[2].Flag);

            Assert.InRange(tags[3].Timestamp, 0, 50);
        }

        protected void AssertTagsAlmostEqual(List<Tag> expected, List<Tag> actual)
        {
            Assert.Single(actual.Where(x => x.Type == TagType.Script));
            Assert.Single(actual.Where(x => x.Type == TagType.Audio && x.Flag == TagFlag.Header));
            Assert.Single(actual.Where(x => x.Type == TagType.Video && x.Flag == (TagFlag.Header | TagFlag.Keyframe)));

            for (var i = 0; i < expected.Count; i++)
            {
                var a = expected[i];
                var b = actual[i];

                Assert.NotSame(a, b);
                Assert.Equal(a.Type, b.Type);
                Assert.Equal(a.Flag, b.Flag);

                if (a.IsScript())
                {
                    Assert.Equal(0, b.Timestamp);
                }
                else if (a.IsEnd())
                {
                }
                else if (a.IsHeader())
                {
                    Assert.Equal(0, b.Timestamp);
                    var binaryDataForSerializationUseOnly = a.BinaryDataForSerializationUseOnly;
                    Assert.False(string.IsNullOrWhiteSpace(binaryDataForSerializationUseOnly));
                    Assert.Equal(binaryDataForSerializationUseOnly, b.BinaryDataForSerializationUseOnly);
                }
                else
                {
                    Assert.Equal(a.Timestamp, b.Timestamp);
                    Assert.Equal(a.Index, b.Index);
                }
            }
        }
    }
}
