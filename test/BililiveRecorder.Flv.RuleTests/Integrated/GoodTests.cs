using System.Collections.Generic;
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
        [SampleFileTestData("samples/good")]
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
            this.AssertTagsShouldPassBasicChecks(file);
            this.AssertTagsAlmostEqual(original, file);
            await this.AssertTagsByRerunPipeline(file).ConfigureAwait(false);
        }

        [Theory]
        [SampleFileTestData("samples/good")]
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
            this.AssertTagsShouldPassBasicChecks(file);
            this.AssertTagsAlmostEqual(original, file);
            await this.AssertTagsByRerunPipeline(file).ConfigureAwait(false);
        }
    }
}
