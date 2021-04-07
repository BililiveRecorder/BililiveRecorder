using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Xml;
using Newtonsoft.Json;
using Xunit;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public class BadTests : TestBase
    {
        [Theory]
        [SampleDirectoryTestData("samples/bad")]
        public async Task TestBadSamples(string path)
        {
            // Arrange
            var path_info = Path.Combine(path, "info.json");
            var info = JsonConvert.DeserializeObject<Info>(File.ReadAllText(path_info));

            var path_input = Path.Combine(path, "input.xml");
            var input = this.LoadFile(path_input);

            var reader = new TagGroupReader(new FlvTagListReader(input.Tags));
            var output = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await this.RunPipeline(reader, output, comments).ConfigureAwait(false);

            // Assert
            Assert.Equal(info.AlternativeHeaderCount, output.AlternativeHeaders.Count);

            comments.RemoveAll(x => x.T == CommentType.Logging);

            Assert.Equal(info.AllowedComments.Values.Sum(x => x), comments.Count);
            Assert.DoesNotContain(comments, x => !info.AllowedComments.ContainsKey(x.T));
            Assert.True(info.AllowedComments.All(x => x.Value == comments.Count(c => c.T == x.Key)));

            Assert.Equal(info.Files.Length, output.Files.Count);
            for (var i = 0; i < info.Files.Length; i++)
            {
                var expected = info.Files[i];
                var actual = output.Files[i];

                Assert.Equal(expected.TagCount, actual.Count);

                this.AssertTagsShouldPassBasicChecks(actual);

                if (expected.VideoHeaderData is not null)
                    Assert.Equal(expected.VideoHeaderData, actual[1].BinaryDataForSerializationUseOnly);

                if (expected.AudioHeaderData is not null)
                    Assert.Equal(expected.AudioHeaderData, actual[2].BinaryDataForSerializationUseOnly);

                await this.AssertTagsByRerunPipeline(actual).ConfigureAwait(false);
            }
        }

        public class Info
        {
            public OutputFile[] Files { get; set; } = Array.Empty<OutputFile>();

            public Dictionary<CommentType, int> AllowedComments { get; set; } = new Dictionary<CommentType, int>();

            public int AlternativeHeaderCount { get; set; }
        }

        public class OutputFile
        {
            public string? VideoHeaderData { get; set; }

            public string? AudioHeaderData { get; set; }

            public int TagCount { get; set; }
        }
    }
}
