using System.Net.Http;

namespace BililiveRecorder.Core.Api
{
    public interface IHttpClientAccessor
    {
        HttpClient MainHttpClient { get; }
    }
}
