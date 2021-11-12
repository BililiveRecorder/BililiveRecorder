using System.Collections.Generic;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests.RuleTests
{
    [UsesVerify]
    [ExpectationPath("Good")]
    public class IntegratedGoodTests : IntegratedTestBase
    {
        [Theory]
        [Expectation("StandardTest")]
        [SampleFileTestData("TestData/Good", "*.xml")]
        public async Task StrictTestsAsync(string path)
        {
            // Arrange
            var originalTags = SampleFileLoader.LoadXmlFlv(path).Tags;
            var reader = new TagGroupReader(new FlvTagListReader(SampleFileLoader.LoadXmlFlv(path).Tags));
            var flvTagListWriter = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await RunPipeline(reader, flvTagListWriter, comments).ConfigureAwait(false);

            // Assert
            comments.RemoveAll(x => x.T == CommentType.Logging);

            Assert.Empty(comments);

            Assert.Empty(flvTagListWriter.AlternativeHeaders);

            var outputTags = Assert.Single(flvTagListWriter.Files);
            Assert.Equal(originalTags.Count, outputTags.Count);

            AssertTags.ShouldHaveLinearTimestamps(outputTags);
            AssertTags.ShouldHaveFullHeaderTags(outputTags);
            AssertTags.ShouldHaveSingleHeaderTagPerType(outputTags);
            AssertTags.ShouldAlmostEqual(originalTags, outputTags);

            await AssertTagsByRerunPipeline(outputTags).ConfigureAwait(false);

            var xmlStr = outputTags.SerializeXml();
            await Verifier.Verify(xmlStr).UseExtension("xml").UseParameters(path);
        }

        [Theory]
        [Expectation("WithOffsetTest")]
        [SampleFileTestData("TestData/Good", "*.xml")]
        public async Task StrictWithArtificalOffsetTestsAsync(string path)
        {
            // Arrange
            var originalTags = SampleFileLoader.LoadXmlFlv(path).Tags;

            var random = new System.Random();
            var offset = random.Next(51, 9999);
            if (random.Next(2) == 1)
                offset = -offset;

            var inputTagsWithOffset = SampleFileLoader.LoadXmlFlv(path).Tags;
            foreach (var tag in inputTagsWithOffset)
                tag.Timestamp += offset;
            var reader = new TagGroupReader(new FlvTagListReader(inputTagsWithOffset));

            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await RunPipeline(reader, output, comments).ConfigureAwait(false);

            // Assert
            comments.RemoveAll(x => x.T == CommentType.Logging);
            Assert.Equal(CommentType.TimestampJump, Assert.Single(comments).T);

            Assert.Empty(output.AlternativeHeaders);

            var outputTags = Assert.Single(output.Files);
            Assert.Equal(originalTags.Count, outputTags.Count);

            AssertTags.ShouldHaveLinearTimestamps(outputTags);
            AssertTags.ShouldHaveFullHeaderTags(outputTags);
            AssertTags.ShouldHaveSingleHeaderTagPerType(outputTags);
            AssertTags.ShouldAlmostEqual(originalTags, outputTags);

            await AssertTagsByRerunPipeline(outputTags).ConfigureAwait(false);

            var xmlStr = outputTags.SerializeXml();
            await Verifier.Verify(xmlStr).UseExtension("xml").UseParameters(path);
        }
    }
}
