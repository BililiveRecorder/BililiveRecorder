using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Core.UnitTests
{
    [UsesVerify]
    public class PublicApi
    {
        [Fact]
        public Task HasNoChangesAsync()
        {
            var publicApi = typeof(Recorder).Assembly.GeneratePublicApi();
            return Verifier.Verify(publicApi);
        }
    }
}
