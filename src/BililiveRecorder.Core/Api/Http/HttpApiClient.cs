using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config.V3;
using Flurl;
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
        internal const string HttpHeaderUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36 Edg/132.0.0.0";
        private static readonly Regex matchCookieUidRegex = new Regex(@"DedeUserID=(\d+?);?(?=\b|$)", RegexOptions.Compiled);
        private static readonly Regex matchCookieBuvid3Regex = new Regex(@"buvid3=(.+?);?(?=\b|$)", RegexOptions.Compiled);
        private long uid;
        private string? buvid3;

        private readonly GlobalConfig config;

        private readonly Wbi wbi = new Wbi();
        private DateTimeOffset wbiLastUpdate = DateTimeOffset.MinValue;
        private static readonly TimeSpan wbiUpdateInterval = TimeSpan.FromHours(4);

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
                this.buvid3 = Buvid.GenerateLocalId();
                headers.Add("Cookie", $"buvid3={this.buvid3}");
            }

            var old = Interlocked.Exchange(ref this.client, client);
            old?.Dispose();
        }

        private void Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is (nameof(this.config.Cookie)) or (nameof(this.config.TimingApiTimeout)))
                this.UpdateHttpClient();
        }

        private readonly SemaphoreSlim wbiSemaphoreSlim = new SemaphoreSlim(1, 1);

        private async Task UpdateWbiKeyAsync()
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            if (this.wbiLastUpdate + wbiUpdateInterval > DateTimeOffset.UtcNow)
                return;

            await this.wbiSemaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (this.wbiLastUpdate + wbiUpdateInterval > DateTimeOffset.UtcNow)
                    return;

                const string URL = @"https://api.bilibili.com/x/web-interface/nav";
                var resp = await this.client.GetAsync(URL).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jo = JObject.Parse(text);

                var wbi_img = (jo["data"]?["wbi_img"]) ?? throw new Exception("failed to get wbi key");
                var img_url = wbi_img["img_url"]?.ToObject<string>() ?? throw new Exception("failed to get wbi key");
                var sub_url = wbi_img["sub_url"]?.ToObject<string>() ?? throw new Exception("failed to get wbi key");

                string img, sub;

                {
                    var slash = img_url.LastIndexOf('/');
                    var dot = img_url.IndexOf('.', slash);
                    if (slash == -1 || dot == -1)
                        throw new Exception("failed to get wbi key");
                    img = img_url.Substring(slash + 1, dot - slash - 1);
                }

                {
                    var slash = sub_url.LastIndexOf('/');
                    var dot = sub_url.IndexOf('.', slash);
                    if (slash == -1 || dot == -1)
                        throw new Exception("failed to get wbi key");
                    sub = sub_url.Substring(slash + 1, dot - slash - 1);
                }

                if (string.IsNullOrWhiteSpace(img) || string.IsNullOrWhiteSpace(sub))
                    throw new Exception("failed to get wbi key");

                this.wbi.UpdateKey(img, sub);
                this.wbiLastUpdate = DateTimeOffset.UtcNow;

            }
            finally
            {
                this.wbiSemaphoreSlim.Release();
            }
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

            await this.UpdateWbiKeyAsync().ConfigureAwait(false);

            Url url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getInfoByRoom?room_id={roomid}&web_location=444.8";
            var q = url.QueryParams;

            var sign = this.wbi.Sign(q.Select(static x => new KeyValuePair<string, string>(x.Name, x.Value?.ToString() ?? string.Empty)));

            q.AddOrReplace(Wbi.W_RID, sign.sign);
            q.AddOrReplace(Wbi.WTS, sign.ts);

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

            Url url = $@"{this.config.LiveApiHost}/xlive/web-room/v2/index/getRoomPlayInfo?room_id=0&no_playurl=0&mask=1&qn=0&platform=web&protocol=0,1&format=0,1,2&codec=0,1,2&dolby=5&panorama=1&hdr_type=0,1&web_location=444.8";
            var q = url.QueryParams;

            q.AddOrReplace("room_id", roomid);
            q.AddOrReplace("qn", qn);

            var sign = this.wbi.Sign(q.Select(static x => new KeyValuePair<string, string>(x.Name, x.Value?.ToString() ?? string.Empty)));
            q.AddOrReplace(Wbi.W_RID, sign.sign);
            q.AddOrReplace(Wbi.WTS, sign.ts);

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

        public async Task<BilibiliApiResponse<DanmuInfo>> GetDanmakuServerAsync(int roomid)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(HttpApiClient));

            await this.UpdateWbiKeyAsync().ConfigureAwait(false);

            Url url = $@"{this.config.LiveApiHost}/xlive/web-room/v1/index/getDanmuInfo?id={roomid}&type=0&web_location=444.8";
            var q = url.QueryParams;

            var sign = this.wbi.Sign(q.Select(static x => new KeyValuePair<string, string>(x.Name, x.Value?.ToString() ?? string.Empty)));
            q.AddOrReplace(Wbi.W_RID, sign.sign);
            q.AddOrReplace(Wbi.WTS, sign.ts);

            return await this.FetchAsync<DanmuInfo>(url);
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
                    this.wbiSemaphoreSlim.Dispose();
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
