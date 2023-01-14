using System.Threading.Tasks;
using PublicApiGenerator;
using VerifyXunit;
using Xunit;

namespace BililiveRecorder.Flv.Tests
{
    [UsesVerify]
    public class PublicApi
    {
        [Fact]
        public Task HasNoChangesAsync()
        {
            var publicApi = typeof(Tag).Assembly.GeneratePublicApi(new ApiGeneratorOptions { ExcludeAttributes = new[] { "System.Runtime.Versioning.TargetFrameworkAttribute" } });
            return Verifier.Verify(publicApi);
        }
    }
}
