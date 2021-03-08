using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Xml;
using Xunit;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public class GoodTests : TestBase
    {
        [Theory]
        [SampleFileTestData("samples/good-strict")]
        public async Task StrictTestsAsync(string path)
        {
            // Arrange
            var original = this.LoadFile(path).Tags;
            var reader = new TagGroupReader(new FlvTagListReader(this.LoadFile(path).Tags));
            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await this.RunPipeline(reader, output, comments).ConfigureAwait(false);

            // Assert
            Assert.Empty(comments);

            Assert.Empty(output.AlternativeHeaders);

            var file = Assert.Single(output.Files);
            Assert.Equal(original.Count, file.Count);
            AssertTags(original, file);
        }

        [Theory]
        [SampleFileTestData("samples/good-strict")]
        public async Task StrictWithArtificalOffsetTestsAsync(string path)
        {
            // Arrange
            var original = this.LoadFile(path).Tags;

            var offset = new System.Random().Next(-9999, 9999);
            var inputTags = this.LoadFile(path).Tags;
            foreach (var tag in inputTags)
                tag.Timestamp += offset;
            var reader = new TagGroupReader(new FlvTagListReader(inputTags));

            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await this.RunPipeline(reader, output, comments).ConfigureAwait(false);

            // Assert
            Assert.Equal(CommentType.TimestampJump, Assert.Single(comments).CommentType);

            Assert.Empty(output.AlternativeHeaders);

            var file = Assert.Single(output.Files);
            Assert.Equal(original.Count, file.Count);
            AssertTags(original, file);
        }

        private static void AssertTags(List<Tag> original, List<Tag> file)
        {
            Assert.Single(file.Where(x => x.Type == TagType.Script));
            Assert.Single(file.Where(x => x.Type == TagType.Audio && x.Flag == TagFlag.Header));
            Assert.Single(file.Where(x => x.Type == TagType.Video && x.Flag == (TagFlag.Header | TagFlag.Keyframe)));

            for (var i = 0; i < original.Count; i++)
            {
                var a = original[i];
                var b = file[i];

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

        //[Theory(Skip = "no data yet")]
        //[SampleFileTestData("samples/good-relax")]
        //public void RelaxTests(string path)
        //{

        //}
    }
}
