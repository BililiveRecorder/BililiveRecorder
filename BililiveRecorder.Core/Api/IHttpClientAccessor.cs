using System.Net.Http;
using System.Threading.Tasks;

namespace BililiveRecorder.Core.Api
{
    public interface IHttpClientAccessor
    {
        Task<(bool, string)> TestCookieAsync();
    }
}
