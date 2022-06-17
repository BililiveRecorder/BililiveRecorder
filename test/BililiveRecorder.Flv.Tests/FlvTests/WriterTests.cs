using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Writer;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests.FlvTests
{
    [UsesVerify]
    [ExpectationPath("FlvWriter")]
    public class WriterTests
    {
        [Theory]
        [Expectation("Output")]
        [SampleFileTestData("../data/flv/TestData/Flv", "*.flv")]
        public async Task ParserOutputIsCurrectAsync(string path)
        {
            var fullPath = SampleFileLoader.GetFullPath(path);

            var testRecyclableMemoryStreamProvider = new TestRecyclableMemoryStreamProvider();
            var reader = new FlvTagPipeReader(PipeReader.Create(File.OpenRead(fullPath)), testRecyclableMemoryStreamProvider, skipData: false, leaveOpen: false, logger: null);

            var msprovider = new MemoryStreamFlvTargetProvider();
            var writer = new FlvTagFileWriter(msprovider, testRecyclableMemoryStreamProvider, null);
            await writer.CreateNewFile();

            while (true)
            {
                var tag = await reader.ReadTagAsync(default).ConfigureAwait(false);
                if (tag is null) break;
                await writer.WriteTag(tag);
            }

            await Verifier.Verify(msprovider.Stream).UseExtension("flv").UseParameters(path);
        }

        public class MemoryStreamFlvTargetProvider : IFlvWriterTargetProvider
        {
            public MemoryStream Stream { get; } = new MemoryStream();

            private bool flag = false;

            public Stream CreateAccompanyingTextLogStream() => throw new System.NotImplementedException();

            public (Stream stream, object? state) CreateOutputStream()
            {
                if (!this.flag)
                    this.flag = true;
                else
                    throw new System.InvalidOperationException();

                return (this.Stream, null);
            }
        }
    }
}
