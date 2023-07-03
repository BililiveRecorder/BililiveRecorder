using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config.V3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BililiveRecorder.Core.Api.Http
{
    internal class HttpApiClient : IApiClient, IDanmakuServerApiClient, IHttpClientAccessor
    {
        internal const string HttpHeaderAccept = "application/json, text/javascript, */*; q=0.01";
        internal const string HttpHeaderReferer = "https://live.bilibili.com/";
        internal const string HttpHeaderOrigin = "https://live.bilibili.com";
        internal const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36";

        private readonly GlobalConfig config;
        private readonly HttpClient anonClient;
        private HttpClient mainClient;
        private bool disposedValue;

        public HttpClient MainHttpClient => this.mainClient;

        public HttpApiClient(GlobalConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            config.PropertyChanged += this.Config_PropertyChanged;

            this.mainClient = null!;
            this.UpdateHttpClient();

            this.anonClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(config.TimingApiTimeout)
            };
            var headers = this.anonClient.DefaultRequestHeaders;
            headers.Add("Accept", HttpHeaderAccept);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);
        }

        private void UpdateHttpClient()
        {
            var client = new HttpClient(new HttpClientHandler
            {
                UseCookies = false,
                UseDefaultCredentials = false,
            })
            {
                Timeout = TimeSpan.FromMilliseconds(this.config.TimingApiTimeout)
            };
            var headers = client.DefaultRequestHeaders;
            headers.Add("Accept", HttpHeaderAccept);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);

            var cookie_string = this.config.Cookie;
            if (!string.IsNullOrWhiteSpace(cookie_string))
                headers.Add("Cookie", cookie_string);

            var old = Interlocked.Exchange(ref this.mainClient, client);
            old?.Dispose();
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is (nameof(this.config.Cookie)) or (nameof(this.config.TimingApiTimeout)))
                this.UpdateHttpClient();
        }

        private static async Task<BilibiliApiResponse<T>> FetchAsync<T>(HttpClient client, string url) where T : class
        {
            // 记得 GetRoomInfoAsync 里复制了一份这里的代码，以后修改记得一起改了

            var resp = await client.GetAsync(url).ConfigureAwait(false);

            if (resp.StatusCode == (HttpStatusCode)412)
                throw new Http412Exception("Got HTTP Status 412 when requesting " + url);

            resp.EnsureSuccessStatusCode();

            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            var obj = JsonConvert.DeserializeObject<BilibiliApiResponse<T>>(text);
            return obj?.Code != 0 ? throw new BilibiliApiResponseCodeNotZeroException(obj?.Code, text) : obj;
        }

        public async Task<BilibiliApiResponse<RoomInfo>> GetRoomInfoAsync(int roomid)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            var url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getInfoByRoom?room_id={roomid}";

            // return FetchAsync<RoomInfo>(this.mainClient, url);
            // 下面的代码是从 FetchAsync 里复制修改的
            // 以后如果修改 FetchAsync 记得把这里也跟着改了

            var resp = await this.mainClient.GetAsync(url).ConfigureAwait(false);

            if (resp.StatusCode == (HttpStatusCode)412)
                throw new Http412Exception("Got HTTP Status 412 when requesting " + url);

            resp.EnsureSuccessStatusCode();

            var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

            var jobject = JObject.Parse(text);

            var obj = jobject.ToObject<BilibiliApiResponse<RoomInfo>>();
            if (obj?.Code != 0)
                throw new BilibiliApiResponseCodeNotZeroException(obj?.Code, text);

            obj.Data!.RawBilibiliApiJsonData = jobject["data"] as JObject;

            return obj;
        }

        public Task<BilibiliApiResponse<RoomPlayInfo>> GetStreamUrlAsync(int roomid, int qn)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            var url = $@"{this.config.LiveApiHost}/xlive/web-room/v2/index/getRoomPlayInfo?room_id={roomid}&protocol=0,1&format=0,1,2&codec=0,1&qn={qn}&platform=web&ptype=8&dolby=5&panorama=1";
            return FetchAsync<RoomPlayInfo>(this.mainClient, url);
        }

        public Task<BilibiliApiResponse<DanmuInfo>> GetDanmakuServerAsync(int roomid)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            var url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getDanmuInfo?id={roomid}&type=0";
            return FetchAsync<DanmuInfo>(this.mainClient, url);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.config.PropertyChanged -= this.Config_PropertyChanged;
                    this.mainClient.Dispose();
                    this.anonClient.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~HttpApiClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
