using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    internal static class BililiveAPI
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Random random = new Random();
        private static readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private static HttpClient httpclient;
        internal static Config.ConfigV1 Config = null; // TODO: 以后有空把整个 class 改成非 static 的然后用 DI 获取 config

        static BililiveAPI()
        {
            httpclient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            httpclient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            httpclient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
            httpclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);
        }

        public static async Task ApplyCookieSettings(string cookie_string)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(cookie_string))
                {
                    try
                    {
                        CookieContainer cc = new CookieContainer();
                        cc.PerDomainCapacity = 300;
                        foreach (var t in cookie_string.Trim(' ', ';').Split(';').Select(x => x.Trim().Split(new[] { '=' }, 2)))
                        {
                            try
                            {
                                string v = string.Empty;
                                if (t.Length == 2)
                                {
                                    v = System.Web.HttpUtility.UrlDecode(t[1]).Trim();
                                }

                                cc.Add(new Cookie(t[0].Trim(), v, "/", ".bilibili.com"));
                            }
                            catch (Exception) { }
                        }

                        var pclient = new HttpClient(handler: new HttpClientHandler
                        {
                            CookieContainer = cc
                        }, disposeHandler: true)
                        {
                            Timeout = TimeSpan.FromSeconds(5)
                        };
                        pclient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                        pclient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
                        pclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);

                        httpclient = pclient;
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "设置 Cookie 时发生错误");

                    }
                }

                var cleanclient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                cleanclient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
                cleanclient.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
                cleanclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);
                httpclient = cleanclient;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// 下载json并解析
        /// </summary>
        /// <param name="url">下载路径</param>
        /// <returns>数据</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="WebException"/>
        public static async Task<JObject> HttpGetJsonAsync(string url)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                var s = await httpclient.GetStringAsync(url);
                var j = JObject.Parse(s);
                return j;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        /// <summary>
        /// 获取直播间播放地址
        /// </summary>
        /// <param name="roomid">原房间号</param>
        /// <returns>FLV播放地址</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public static async Task<string> GetPlayUrlAsync(int roomid)
        {
            string url = $@"https://api.live.bilibili.com/room/v1/Room/playUrl?cid={roomid}&quality=4&platform=web";
            if (Config.AvoidTxy)
            {
                // 尽量避开腾讯云
                int attempt_left = 3;
                while (true)
                {
                    attempt_left--;
                    if ((await HttpGetJsonAsync(url))?["data"]?["durl"] is JArray all_jtoken && all_jtoken.Count > 0)
                    {
                        var all = all_jtoken.Select(x => x["url"].ToObject<string>()).ToArray();
                        var withoutTxy = all.Where(x => !x.Contains("txy.")).ToArray();
                        if (withoutTxy.Length > 0)
                        {
                            return withoutTxy[random.Next(withoutTxy.Length)];
                        }
                        else if (attempt_left <= 0)
                        {
                            return all[random.Next(all.Length)];
                        }
                    }
                    else
                    {
                        throw new Exception("没有直播播放地址");
                    }
                }
            }
            else
            {
                // 随机选择一个 url
                if ((await HttpGetJsonAsync(url))?["data"]?["durl"] is JArray array)
                {
                    var urls = array.Select(t => t?["url"]?.ToObject<string>());
                    var distinct = urls.Distinct().ToArray();
                    if (distinct.Length > 0)
                    {
                        return distinct[random.Next(distinct.Length)];
                    }
                }
                throw new Exception("没有直播播放地址");
            }
        }

        /// <summary>
        /// 获取直播间信息
        /// </summary>
        /// <param name="roomid">房间号（允许短号）</param>
        /// <returns>直播间信息</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public static async Task<RoomInfo> GetRoomInfoAsync(int roomid)
        {
            try
            {
                var room = await HttpGetJsonAsync($@"https://api.live.bilibili.com/room/v1/Room/get_info?id={roomid}");
                if (room?["code"]?.ToObject<int>() != 0)
                {
                    logger.Warn("不能获取 {roomid} 的信息1: {errormsg}", roomid, room?["message"]?.ToObject<string>() ?? "网络超时");
                    return null;
                }

                var user = await HttpGetJsonAsync($@"https://api.live.bilibili.com/live_user/v1/UserInfo/get_anchor_in_room?roomid={roomid}");
                if (user?["code"]?.ToObject<int>() != 0)
                {
                    logger.Warn("不能获取 {roomid} 的信息2: {errormsg}", roomid, user?["message"]?.ToObject<string>() ?? "网络超时");
                    return null;
                }

                var i = new RoomInfo()
                {
                    ShortRoomId = room?["data"]?["short_id"]?.ToObject<int>() ?? throw new Exception("未获取到直播间信息"),
                    RoomId = room?["data"]?["room_id"]?.ToObject<int>() ?? throw new Exception("未获取到直播间信息"),
                    IsStreaming = 1 == (room?["data"]?["live_status"]?.ToObject<int>() ?? throw new Exception("未获取到直播间信息")),
                    UserName = user?["data"]?["info"]?["uname"]?.ToObject<string>() ?? throw new Exception("未获取到直播间信息"),
                    Title = room?["data"]?["title"]?.ToObject<string>() ?? throw new Exception("未获取到直播间信息")
                };
                return i;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "获取直播间 {roomid} 的信息时出错", roomid);
                throw;
            }
        }
    }
}
