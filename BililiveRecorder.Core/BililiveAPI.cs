using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace BililiveRecorder.Core
{
    internal static class BililiveAPI
    {
        /// <summary>
        /// 下载json并解析
        /// </summary>
        /// <param name="url">下载路径</param>
        /// <returns>数据</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="WebException"/>
        public static JObject HttpGetJson(string url)
        {
            var c = new WebClient();
            c.Headers.Add(HttpRequestHeader.UserAgent, Utils.UserAgent);
            c.Headers.Add(HttpRequestHeader.Accept, "application/json, text/javascript, */*; q=0.01");
            c.Headers.Add(HttpRequestHeader.Referer, "https://live.bilibili.com/");
            var s = c.DownloadString(url);
            var j = JObject.Parse(s);
            return j;
        }

        /// <summary>
        /// 获取直播间播放地址
        /// </summary>
        /// <param name="roomid">原房间号</param>
        /// <returns>FLV播放地址</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public static string GetPlayUrl(int roomid)
        {
            string url = $@"https://api.live.bilibili.com/room/v1/Room/playUrl?cid={roomid}&quality=0&platform=web";
            if (HttpGetJson(url)?["data"]?["durl"] is JArray array)
            {
                List<string> urls = new List<string>();
                for (int i = 0; i < array.Count; i++)
                {
                    urls.Add(array[i]?["url"]?.ToObject<string>());
                }
                var distinct = urls.Distinct().ToArray();
                if (distinct.Length > 0)
                {
                    return distinct[random.Next(0, distinct.Count() - 1)];
                }
            }
            throw new Exception("没有直播播放地址");
        }

        /// <summary>
        /// 获取直播间信息
        /// </summary>
        /// <param name="roomid">房间号（允许短号）</param>
        /// <returns>直播间信息</returns>
        /// <exception cref="WebException"/>
        /// <exception cref="Exception"/>
        public static RoomInfo GetRoomInfo(int roomid)
        {
            string url = $@"https://api.live.bilibili.com/AppRoom/index?room_id={roomid}&platform=android";
            var data = HttpGetJson(url);
            var i = new RoomInfo()
            {
                DisplayRoomid = data?["data"]?["show_room_id"]?.ToObject<int>() ?? throw new Exception("未获取到直播间信息"),
                RealRoomid = (int)(data?["data"]?["room_id"]?.ToObject<int>() ?? throw new Exception("未获取到直播间信息")),
                Username = data?["data"]?["uname"]?.ToObject<string>() ?? throw new Exception("未获取到直播间信息"),
                isStreaming = "LIVE" == (data?["data"]?["status"]?.ToObject<string>() ?? throw new Exception("未获取到直播间信息")),
            };
            return i;
        }

        private static readonly Regex rx = new Regex(@"\\[uU]([0-9A-Fa-f]{4})", RegexOptions.Compiled);
        internal static string Decode(this string str) => rx.Replace(str, match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
        private static readonly Random random = new Random();
    }
}
