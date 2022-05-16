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

namespace BililiveRecorder.Flv.Tests.RuleTests
{
    [UsesVerify]
    [ExpectationPath("Bad")]
    public class IntegratedBadTests : IntegratedTestBase
    {
        [Theory]
        [Expectation("TestBadSamples")]
        [SampleFileTestData("TestData/Bad", "*.xml")]
        public async Task TestBadSamples(string path)
        {

            var originalTags = SampleFileLoader.LoadXmlFlv(path).Tags;
            var reader = new TagGroupReader(new FlvTagListReader(originalTags));
            var flvTagListWriter = new FlvTagListWriter();
            var comments = new List<ProcessingComment>();

            // Act
            await RunPipeline(reader, flvTagListWriter, comments).ConfigureAwait(false);

            // Assert
            comments.RemoveAll(x => x.T == CommentType.Logging);

            var outputResult = new OutputResult
            {
                AlternativeHeaders = flvTagListWriter.AlternativeHeaders.Select(x => x.BinaryDataForSerializationUseOnly).ToArray(),
                Comments = comments.GroupBy(x => x.T).Select(x => new CommentCount(x.Key, x.Count())).ToArray(),
                TagCounts = flvTagListWriter.Files.Select(x => x.Count).ToArray()
            };

            using var sw = new StringWriter();
            sw.WriteLine(JsonConvert.SerializeObject(outputResult, Formatting.Indented));

            for (var i = 0; i < flvTagListWriter.Files.Count; i++)
            {
                var outputTags = flvTagListWriter.Files[i];

                AssertTags.ShouldHaveLinearTimestamps(outputTags);
                await AssertTagsByRerunPipeline(outputTags).ConfigureAwait(false);

                var xmlStr = outputTags.SerializeXml();
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

            public override bool Equals(object? obj) =>
                obj is CommentCount other &&
                this.Type == other.Type &&
                this.Count == other.Count;

            public override int GetHashCode() => HashCode.Combine(this.Type, this.Count);

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

            public static bool operator ==(CommentCount left, CommentCount right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(CommentCount left, CommentCount right)
            {
                return !(left == right);
            }
        }
    }
}
