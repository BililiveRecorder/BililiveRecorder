using System.Net.Http;
using System.Threading.Tasks;

namespace BililiveRecorder.Core.Api
{
    public interface ICookieTester
    {
        Task<(bool, string)> TestCookieAsync();
    }
}
