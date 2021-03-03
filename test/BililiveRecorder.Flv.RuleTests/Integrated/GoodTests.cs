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
            Assert.Single(output.Files);
            Assert.Equal(original.Count, output.Files[0].Count);

            var file = output.Files[0];
            for (var i = 0; i < original.Count; i++)
            {
                var a = original[i];
                var b = file[i];

                Assert.Equal(a.Type, b.Type);
                Assert.Equal(a.Timestamp, a.Timestamp);
                Assert.Equal(a.Flag, b.Flag);

                if (a.IsHeader())
                {
                    Assert.Equal(a.BinaryDataForSerializationUseOnly, b.BinaryDataForSerializationUseOnly);
                }
                else if (!a.IsScript())
                {
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
