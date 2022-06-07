using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Parser;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests.FlvTests
{
    [UsesVerify]
    [ExpectationPath("FlvParser")]
    public class ParserTest
    {
        [Theory]
        [Expectation("XmlOutput")]
        [SampleFileTestData("../data/flv/TestData/Flv", "*.flv")]
        public async Task ParserOutputIsCurrectAsync(string path)
        {
            var fullPath = SampleFileLoader.GetFullPath(path);
            var tags = new List<Tag>();
            var reader = new FlvTagPipeReader(PipeReader.Create(File.OpenRead(fullPath)), new TestRecyclableMemoryStreamProvider(), skipData: false, leaveOpen: false, logger: null);

            while (true)
            {
                var tag = await reader.ReadTagAsync(default).ConfigureAwait(false);
                if (tag is null) break;
                tags.Add(tag);
            }

            await Verifier.Verify(tags.SerializeXml()).UseExtension("xml").UseParameters(path);
        }
    }
}
