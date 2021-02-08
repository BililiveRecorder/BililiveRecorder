using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Xml;
using JetBrains.dotMemoryUnit;
using Xunit;
using Xunit.Abstractions;

namespace BililiveRecorder.Flv.UnitTests.Flv
{
    public class ParsingTest
    {
        private readonly ITestOutputHelper _output;

        public ParsingTest(ITestOutputHelper output)
        {
            this._output = output;
            DotMemoryUnitTestOutput.SetOutputMethod(this._output.WriteLine);
        }

        // [AssertTraffic(AllocatedSizeInBytes = 1000)]
        [Fact(Skip = "Not ready")]
        public async Task Run()
        {
            var path = @"";

            var tags = new List<Tag>();

            var reader = new FlvTagPipeReader(PipeReader.Create(File.OpenRead(path)), new TestRecyclableMemoryStreamProvider(), skipData: true);

            while (true)
            {
                var tag = await reader.ReadTagAsync().ConfigureAwait(false);

                if (tag is null)
                    break;

                tags.Add(tag);
            }

            var xmlObj = new XmlFlvFile
            {
                Tags = tags
            };

            var writer = new StringWriter();
            XmlFlvFile.Serializer.Serialize(writer, xmlObj);

            var xmlStr = writer.ToString();

            //var peakWorkingSet = Process.GetCurrentProcess().PeakWorkingSet64;
            //throw new System.Exception("PeakWorkingSet64: " + peakWorkingSet);
        }
    }
}
