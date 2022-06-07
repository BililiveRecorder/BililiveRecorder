using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline.Actions;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests.FlvTests
{
    [UsesVerify]
    [ExpectationPath("FlvGrouping")]
    public class GroupingTests
    {
        [Theory]
        [Expectation("GroupingFromFlv")]
        [SampleFileTestData("../data/flv/TestData/Flv", "*.flv")]
        public async void GroupingShouldMatchExpection(string path)
        {
            var results = new List<PipelineAction>();
            var grouping = new TagGroupReader(new FlvTagPipeReader(PipeReader.Create(File.OpenRead(SampleFileLoader.GetFullPath(path))), new TestRecyclableMemoryStreamProvider(), skipData: true, logger: null));

            while (true)
            {
                var g = await grouping.ReadGroupAsync(default);
                if (g is null) break;
                results.Add(g);
            }

            await Verifier.Verify(results).UseParameters(path);
        }
    }
}
