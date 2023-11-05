using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config.V3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BililiveRecorder.Core.Api.Http
{
    internal class HttpApiClient : IApiClient, IDanmakuServerApiClient, ICookieTester
    {
        internal const string HttpHeaderAccept = "application/json, text/javascript, */*; q=0.01";
        internal const string HttpHeaderAcceptLanguage = "zh-CN";
        internal const string HttpHeaderReferer = "https://live.bilibili.com/";
        internal const string HttpHeaderOrigin = "https://live.bilibili.com";
        internal const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36";
        private static readonly Regex matchCookieUidRegex = new Regex(@"DedeUserID=(\d+?);", RegexOptions.Compiled);
        private static readonly Regex matchCookieBuvid3Regex = new Regex(@"buvid3=(.+?);", RegexOptions.Compiled);
        private long uid;
        private string? buvid3;

        private readonly GlobalConfig config;
        private HttpClient client;
        private bool disposedValue;

        public HttpApiClient(GlobalConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            config.PropertyChanged += this.Config_PropertyChanged;

            this.client = null!;
            this.UpdateHttpClient();
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
            headers.Add("Accept-Language", HttpHeaderAcceptLanguage);
            headers.Add("Origin", HttpHeaderOrigin);
            headers.Add("Referer", HttpHeaderReferer);
            headers.Add("User-Agent", HttpHeaderUserAgent);

            var cookie_string = this.config.Cookie;
            if (!string.IsNullOrWhiteSpace(cookie_string))
            {
                headers.Add("Cookie", cookie_string);
                _ = long.TryParse(matchCookieUidRegex.Match(cookie_string).Groups[1].Value, out var uid);
                this.uid = uid;
                var buvid3 = matchCookieBuvid3Regex.Match(cookie_string).Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(buvid3))
                    this.buvid3 = buvid3;
                else
                    this.buvid3 = null;
            }
            else
            {
                this.uid = 0;
                this.buvid3 = null;
            }

            var old = Interlocked.Exchange(ref this.client, client);
            old?.Dispose();
        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is (nameof(this.config.Cookie)) or (nameof(this.config.TimingApiTimeout)))
                this.UpdateHttpClient();
        }

        private async Task<string> FetchAsTextAsync(string url)
        {
            var resp = await this.client.GetAsync(url).ConfigureAwait(false);

            if (resp.StatusCode == (HttpStatusCode)412)
                throw new Http412Exception("Got HTTP Status 412 when requesting " + url);

            resp.EnsureSuccessStatusCode();

            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private async Task<BilibiliApiResponse<T>> FetchAsync<T>(string url) where T : class
        {
            var text = await this.FetchAsTextAsync(url).ConfigureAwait(false);
            var obj = JsonConvert.DeserializeObject<BilibiliApiResponse<T>>(text);
            return obj?.Code != 0 ? throw new BilibiliApiResponseCodeNotZeroException(obj?.Code, text) : obj;
        }

        public async Task<BilibiliApiResponse<RoomInfo>> GetRoomInfoAsync(int roomid)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            var url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getInfoByRoom?room_id={roomid}";

            var text = await this.FetchAsTextAsync(url).ConfigureAwait(false);

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
            return this.FetchAsync<RoomPlayInfo>(url);
        }

        public async Task<(bool, string)> TestCookieAsync()
        {
            // 需要测试 cookie 的情况不需要风控和失败检测
            var resp = await this.client.GetStringAsync("https://api.live.bilibili.com/xlive/web-ucenter/user/get_user_info").ConfigureAwait(false);
            var jo = JObject.Parse(resp);
            if (jo["code"]?.ToObject<int>() != 0)
                return (false, $"Response:\n{resp}");

            var message = $@"User: {jo["data"]?["uname"]?.ToObject<string>()}
UID (from API response): {jo["data"]?["uid"]?.ToObject<string>()}
UID (from Cookie): {this.GetUid()}
BUVID3 (from Cookie): {this.GetBuvid3()}";
            return (true, message);
        }

        public long GetUid() => this.uid;

        public string? GetBuvid3() => this.buvid3;

        public Task<BilibiliApiResponse<DanmuInfo>> GetDanmakuServerAsync(int roomid)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            var url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getDanmuInfo?id={roomid}&type=0";
            return this.FetchAsync<DanmuInfo>(url);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.config.PropertyChanged -= this.Config_PropertyChanged;
                    this.client.Dispose();
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
