using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VerifyTests;
using VerifyXunit;
using Xunit;
namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    [UsesVerify]
    [ExpectationPath("Bad")]
    public class IntegratedBadTests : IntegratedTestBase
    {
        [Theory]
        [Expectation("TestBadSamples")]
        //[SampleDirectoryTestData("TestData/Bad")]
        [SampleFileTestData("TestData/Bad")]
        public async Task TestBadSamples(string path)
        {
            // Arrange
            //var path_info = Path.Combine(path, "info.json");
            //var INFO_TO_BE_REMOVED = JsonConvert.DeserializeObject<Info>(File.ReadAllText(path_info));

            //var path_input = Path.Combine(path, "input.xml");
            var originalTags = SampleFileLoader.Load(path).Tags;
            var reader = new TagGroupReader(new FlvTagListReader(originalTags));
            var flvTagListWriter = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await RunPipeline(reader, flvTagListWriter, comments).ConfigureAwait(false);

            // Assert
            comments.RemoveAll(x => x.T == CommentType.Logging);

            var outputResult = new OutputResult();

            //Assert.Equal(INFO_TO_BE_REMOVED.AlternativeHeaderCount, flvTagListWriter.AlternativeHeaders.Count);
            outputResult.AlternativeHeaders = flvTagListWriter.AlternativeHeaders.Select(x => x.BinaryDataForSerializationUseOnly).ToArray();

            //Assert.Equal(INFO_TO_BE_REMOVED.AllowedComments.Values.Sum(x => x), comments.Count);
            //Assert.DoesNotContain(comments, x => !INFO_TO_BE_REMOVED.AllowedComments.ContainsKey(x.T));
            //Assert.True(INFO_TO_BE_REMOVED.AllowedComments.All(x => x.Value == comments.Count(c => c.T == x.Key)));

            outputResult.Comments = comments.GroupBy(x => x.T).Select(x => new CommentCount(x.Key, x.Count())).ToArray();


            //Assert.Equal(INFO_TO_BE_REMOVED.Files.Length, flvTagListWriter.Files.Count);

            outputResult.TagCounts = flvTagListWriter.Files.Select(x => x.Count).ToArray();

            // outputResult.Tags = flvTagListWriter.Files.ToArray();

            using var sw = new StringWriter();

            sw.WriteLine(JsonConvert.SerializeObject(outputResult, Formatting.Indented));

            // OutputResultSerializer.Serialize(xmlSw, outputResult);
            // var xmlStr = xmlSw.ToString();

            // await Verifier.Verify(xmlStr).UseParameters(path);

            for (var i = 0; i < flvTagListWriter.Files.Count; i++)
            {
                // var expected = INFO_TO_BE_REMOVED.Files[i];
                var outputTags = flvTagListWriter.Files[i];

                // Assert.Equal(expected.TagCount, outputTags.Count);

                // TODO 重写相关测试的检查，支持只有音频或视频 header 的数据片段

                AssertTags.ShouldHaveLinearTimestamps(outputTags);

                // if (!expected.SkipTagCheck)
                //   this.AssertTagsShouldPassBasicChecks(outputTags);

                //if (expected.VideoHeaderData is not null)
                //    Assert.Equal(expected.VideoHeaderData, outputTags[1].BinaryDataForSerializationUseOnly);

                //if (expected.AudioHeaderData is not null)
                //    Assert.Equal(expected.AudioHeaderData, outputTags[2].BinaryDataForSerializationUseOnly);

                await AssertTagsByRerunPipeline(outputTags).ConfigureAwait(false);

                var xmlStr = SerializeTags(outputTags);

                // await Verifier.Verify(xmlStr).UseExtension("xml").UseParameters(path);

                sw.WriteLine(xmlStr);
            }

            await Verifier.Verify(sw.ToString()).UseParameters(path);
        }

        public static XmlSerializer OutputResultSerializer { get; } = new XmlSerializer(typeof(OutputResult));

        public class OutputResult
        {
            public int[] TagCounts { get; set; } = Array.Empty<int>();

            public CommentCount[] Comments { get; set; } = Array.Empty<CommentCount>();

            public string?[] AlternativeHeaders { get; set; } = Array.Empty<string>();
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


        public struct CommentCount
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public CommentType Type;
            public int Count;

            public CommentCount(CommentType type, int count)
            {
                this.Type = type;
                this.Count = count;
            }

            public override bool Equals(object? obj)
            {
                return obj is CommentCount other &&
                       this.Type == other.Type &&
                       this.Count == other.Count;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(this.Type, this.Count);
            }

            public void Deconstruct(out CommentType type, out int count)
            {
                type = this.Type;
                count = this.Count;
            }

            public static implicit operator (CommentType, int)(CommentCount value)
            {
                return (value.Type, value.Count);
            }

            public static implicit operator CommentCount((CommentType, int) value)
            {
                return new CommentCount(value.Item1, value.Item2);
            }
        }
    }
}
