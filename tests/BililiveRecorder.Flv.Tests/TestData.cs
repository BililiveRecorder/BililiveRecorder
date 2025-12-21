using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests
{
    [UsesVerify]
    public class TestData
    {
        [Fact]
        public Task MeetsExpectations()
        {
            var baseDirectory = new DirectoryInfo(Path.Combine(AttributeReader.GetProjectDirectory(), "../data/flv/TestData"));
            var allFiles = baseDirectory.EnumerateFiles("*", new EnumerationOptions()
            {
                RecurseSubdirectories = true
            });

            var relativePaths = allFiles.Select(x => Path.GetRelativePath(baseDirectory.FullName, x.FullName))
                                        .Select(x => x.Replace('\\', '/'))
                                        .OrderBy(x => x);

            return Verifier.Verify(string.Join('\n', relativePaths));
        }
    }
}
