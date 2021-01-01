using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V2;
using Newtonsoft.Json.Linq;
using NLog;

namespace BililiveRecorder.Core
{
    public class BililiveAPI
    {
        private const string HTTP_HEADER_ACCEPT = "application/json, text/javascript, */*; q=0.01";
        private const string HTTP_HEADER_REFERER = "https://live.bilibili.com/";
        private const string DEFAULT_SERVER_HOST = "broadcastlv.chat.bilibili.com";
        private const int DEFAULT_SERVER_PORT = 2243;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Random random = new Random();

        private readonly GlobalConfig globalConfig;
        private readonly HttpClient danmakuhttpclient;
        private HttpClient httpclient;

        public BililiveAPI(GlobalConfig globalConfig)
        {
            this.globalConfig = globalConfig;
            this.globalConfig.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(this.globalConfig.Cookie))
                    this.ApplyCookieSettings(this.globalConfig.Cookie);
            };
            this.ApplyCookieSettings(this.globalConfig.Cookie);

            this.danmakuhttpclient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            this.danmakuhttpclient.DefaultRequestHeaders.Add("Accept", HTTP_HEADER_ACCEPT);
            this.danmakuhttpclient.DefaultRequestHeaders.Add("Referer", HTTP_HEADER_REFERER);
            this.danmakuhttpclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);
        }

        public void ApplyCookieSettings(string cookie_string)
        {
            try
            {
                logger.Trace("设置 Cookie 信息...");
                if (!string.IsNullOrWhiteSpace(cookie_string))
                {
                    var pclient = new HttpClient(handler: new HttpClientHandler
                    {
                        UseCookies = false,
                        UseDefaultCredentials = false,
                    }, disposeHandler: true)
                    {
                        Timeout = TimeSpan.FromSeconds(10)
                    };
                    pclient.DefaultRequestHeaders.Add("Accept", HTTP_HEADER_ACCEPT);
                    pclient.DefaultRequestHeaders.Add("Referer", HTTP_HEADER_REFERER);
                    pclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);
                    pclient.DefaultRequestHeaders.Add("Cookie", cookie_string);
                    this.httpclient = pclient;
                }
                else
                {
                    var cleanclient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    cleanclient.DefaultRequestHeaders.Add("Accept", HTTP_HEADER_ACCEPT);
                    cleanclient.DefaultRequestHeaders.Add("Referer", HTTP_HEADER_REFERER);
                    cleanclient.DefaultRequestHeaders.Add("User-Agent", Utils.UserAgent);
                    this.httpclient = cleanclient;
                }
                logger.Debug("设置 Cookie 成功");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "设置 Cookie 时发生错误");
            }
        }

        /// <summary>
        /// 下载json并解析
        /// </summary>
        /// <param name="url">下载路径</param>
        /// <returns>数据</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="WebException"/>
        private async Task<JObject> HttpGetJsonAsync(HttpClient client, string url)
        {
            try
            {
                var s = await client.GetStringAsync(url);
                var j = JObject.Parse(s);
                return j;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取直播间播放地址
        /// </summary>
        /// <param name="roomid">原房间号</param>
        /// <returns>FLV播放地址</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public async Task<string> GetPlayUrlAsync(int roomid)
        {
            var url = $@"{this.globalConfig.LiveApiHost}/room/v1/Room/playUrl?cid={roomid}&quality=4&platform=web";
            // 随机选择一个 url
            if ((await this.HttpGetJsonAsync(this.httpclient, url))?["data"]?["durl"] is JArray array)
            {
                var urls = array.Select(t => t?["url"]?.ToObject<string>());
                var distinct = urls.Distinct().ToArray();
                if (distinct.Length > 0)
                {
                    return distinct[random.Next(distinct.Length)];
                }
            }
            return null;
        }

        /// <summary>
        /// 获取直播间信息
        /// </summary>
        /// <param name="roomid">房间号（允许短号）</param>
        /// <returns>直播间信息</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public async Task<RoomInfo> GetRoomInfoAsync(int roomid)
        {
            try
            {
                var room = await this.HttpGetJsonAsync(this.httpclient, $@"https://api.live.bilibili.com/room/v1/Room/get_info?id={roomid}");
                if (room?["code"]?.ToObject<int>() != 0)
                {
                    logger.Warn("不能获取 {roomid} 的信息1: {errormsg}", roomid, room?["message"]?.ToObject<string>() ?? "网络超时");
                    return null;
                }

                var user = await this.HttpGetJsonAsync(this.httpclient, $@"https://api.live.bilibili.com/live_user/v1/UserInfo/get_anchor_in_room?roomid={roomid}");
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

        /// <summary>
        /// 获取弹幕连接信息
        /// </summary>
        /// <param name="roomid"></param>
        /// <returns></returns>
        public async Task<(string token, string host, int port)> GetDanmuConf(int roomid)
        {
            try
            {
                var result = await this.HttpGetJsonAsync(this.danmakuhttpclient, $@"https://api.live.bilibili.com/room/v1/Danmu/getConf?room_id={roomid}&platform=pc&player=web");

                if (result?["code"]?.ToObject<int>() == 0)
                {
                    var token = result?["data"]?["token"]?.ToObject<string>() ?? string.Empty;

                    List<(string host, int port)> servers = new List<(string host, int port)>();

                    if (result?["data"]?["host_server_list"] is JArray host_server_list)
                    {
                        foreach (var host_server_jtoken in host_server_list)
                            if (host_server_jtoken is JObject host_server)
                                servers.Add((host_server["host"]?.ToObject<string>(), host_server["port"]?.ToObject<int>() ?? 0));
                    }

                    if (result?["data"]?["server_list"] is JArray server_list)
                    {
                        foreach (var server_jtoken in server_list)
                            if (server_jtoken is JObject server)
                                servers.Add((server["host"]?.ToObject<string>(), server["port"]?.ToObject<int>() ?? 0));
                    }

                    servers.RemoveAll(x => string.IsNullOrWhiteSpace(x.host) || x.port <= 0 || x.host == DEFAULT_SERVER_HOST);

                    if (servers.Count > 0)
                    {
                        var (host, port) = servers[random.Next(servers.Count)];
                        return (token, host, port);
                    }
                    else
                    {
                        return (token, DEFAULT_SERVER_HOST, DEFAULT_SERVER_PORT);
                    }
                }
                else
                {
                    logger.Warn("获取直播间 {roomid} 的弹幕连接信息时返回了 {code}", roomid, result?["code"]?.ToObject<int>());
                    return (string.Empty, DEFAULT_SERVER_HOST, DEFAULT_SERVER_PORT);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "获取直播间 {roomid} 的弹幕连接信息时出错", roomid);
                return (string.Empty, DEFAULT_SERVER_HOST, DEFAULT_SERVER_PORT);
            }
        }
    }
}
