using System;
using Newtonsoft.Json.Linq;

namespace BililiveRecorder.Core
{
    public enum MsgTypeEnum
    {
        /// <summary>
        /// 彈幕
        /// </summary>
        Comment,
        /// <summary>
        /// 禮物
        /// </summary>
        GiftSend,
        /// <summary>
        /// 歡迎老爷
        /// </summary>
        Welcome,
        /// <summary>
        /// 直播開始
        /// </summary>
        LiveStart,
        /// <summary>
        /// 直播結束
        /// </summary>
        LiveEnd,
        /// <summary>
        /// 其他
        /// </summary>
        Unknown,
        /// <summary>
        /// 欢迎船员
        /// </summary>
        WelcomeGuard,
        /// <summary>
        /// 购买船票（上船）
        /// </summary>
        GuardBuy,
        /// <summary>
        /// SuperChat
        /// </summary>
        SuperChat,
        /// <summary>
        /// 房间信息更新
        /// </summary>
        RoomChange
    }

    public class DanmakuModel
    {
        /// <summary>
        /// 消息類型
        /// </summary>
        public MsgTypeEnum MsgType { get; set; }

        /// <summary>
        /// 房间标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 彈幕內容
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// </list></para>
        /// </summary>
        public string CommentText { get; set; }

        /// <summary>
        /// 彈幕用戶
        /// </summary>
        [Obsolete("请使用 UserName")]
        public string CommentUser
        {
            get { return this.UserName; }
            set { this.UserName = value; }
        }

        /// <summary>
        /// 消息触发者用户名
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// <item><see cref="MsgTypeEnum.GiftSend"/></item>
        /// <item><see cref="MsgTypeEnum.Welcome"/></item>
        /// <item><see cref="MsgTypeEnum.WelcomeGuard"/></item>
        /// <item><see cref="MsgTypeEnum.GuardBuy"/></item>
        /// </list></para>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// SC 价格
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// SC 保持时间
        /// </summary>
        public int SCKeepTime { get; set; }

        /// <summary>
        /// 消息触发者用户ID
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// <item><see cref="MsgTypeEnum.GiftSend"/></item>
        /// <item><see cref="MsgTypeEnum.Welcome"/></item>
        /// <item><see cref="MsgTypeEnum.WelcomeGuard"/></item>
        /// <item><see cref="MsgTypeEnum.GuardBuy"/></item>
        /// </list></para>
        /// </summary>
        public int UserID { get; set; }

        /// <summary>
        /// 用户舰队等级
        /// <para>0 为非船员 1 为总督 2 为提督 3 为舰长</para>
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// <item><see cref="MsgTypeEnum.WelcomeGuard"/></item>
        /// <item><see cref="MsgTypeEnum.GuardBuy"/></item>
        /// </list></para>
        /// </summary>
        public int UserGuardLevel { get; set; }

        /// <summary>
        /// 禮物用戶
        /// </summary>
        [Obsolete("请使用 UserName")]
        public string GiftUser
        {
            get { return this.UserName; }
            set { this.UserName = value; }
        }

        /// <summary>
        /// 禮物名稱
        /// </summary>
        public string GiftName { get; set; }

        /// <summary>
        /// 禮物數量
        /// </summary>
        [Obsolete("请使用 GiftCount")]
        public string GiftNum { get { return this.GiftCount.ToString(); } }

        /// <summary>
        /// 礼物数量
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.GiftSend"/></item>
        /// <item><see cref="MsgTypeEnum.GuardBuy"/></item>
        /// </list></para>
        /// <para>此字段也用于标识上船 <see cref="MsgTypeEnum.GuardBuy"/> 的数量（月数）</para>
        /// </summary>
        public int GiftCount { get; set; }

        /// <summary>
        /// 当前房间的礼物积分（Room Cost）
        /// 因以前出现过不传递rcost的礼物，并且用处不大，所以弃用
        /// </summary>
        [Obsolete("如有需要请自行解析RawData", true)]
        public string Giftrcost { get { return "0"; } set { } }

        /// <summary>
        /// 该用户是否为房管（包括主播）
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// <item><see cref="MsgTypeEnum.GiftSend"/></item>
        /// </list></para>
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// 是否VIP用戶(老爺)
        /// <para>此项有值的消息类型：<list type="bullet">
        /// <item><see cref="MsgTypeEnum.Comment"/></item>
        /// <item><see cref="MsgTypeEnum.Welcome"/></item>
        /// </list></para>
        /// </summary>
        public bool IsVIP { get; set; }

        /// <summary>
        /// <see cref="MsgTypeEnum.LiveStart"/>,<see cref="MsgTypeEnum.LiveEnd"/> 事件对应的房间号
        /// </summary>
        public string RoomID { get; set; }

        /// <summary>
        /// 原始数据, 高级开发用
        /// </summary>
        public string RawData { get; set; }

        /// <summary>
        /// 原始数据, 高级开发用
        /// </summary>
        public JObject RawObj { get; set; }

        /// <summary>
        /// 内部用, JSON数据版本号 通常应该是2
        /// </summary>
        public int JSON_Version { get; set; }

        public DanmakuModel()
        { }

        public DanmakuModel(string JSON)
        {
            this.RawData = JSON;
            this.JSON_Version = 2;

            var obj = JObject.Parse(JSON);
            this.RawObj = obj;
            string cmd = obj["cmd"]?.ToObject<string>();
            switch (cmd)
            {
                case "LIVE":
                    this.MsgType = MsgTypeEnum.LiveStart;
                    this.RoomID = obj["roomid"].ToObject<string>();
                    break;
                case "PREPARING":
                    this.MsgType = MsgTypeEnum.LiveEnd;
                    this.RoomID = obj["roomid"].ToObject<string>();
                    break;
                case "DANMU_MSG":
                    this.MsgType = MsgTypeEnum.Comment;
                    this.CommentText = obj["info"][1].ToObject<string>();
                    this.UserID = obj["info"][2][0].ToObject<int>();
                    this.UserName = obj["info"][2][1].ToObject<string>();
                    this.IsAdmin = obj["info"][2][2].ToObject<string>() == "1";
                    this.IsVIP = obj["info"][2][3].ToObject<string>() == "1";
                    this.UserGuardLevel = obj["info"][7].ToObject<int>();
                    break;
                case "SEND_GIFT":
                    this.MsgType = MsgTypeEnum.GiftSend;
                    this.GiftName = obj["data"]["giftName"].ToObject<string>();
                    this.UserName = obj["data"]["uname"].ToObject<string>();
                    this.UserID = obj["data"]["uid"].ToObject<int>();
                    this.GiftCount = obj["data"]["num"].ToObject<int>();
                    break;
                case "GUARD_BUY":
                    {
                        this.MsgType = MsgTypeEnum.GuardBuy;
                        this.UserID = obj["data"]["uid"].ToObject<int>();
                        this.UserName = obj["data"]["username"].ToObject<string>();
                        this.UserGuardLevel = obj["data"]["guard_level"].ToObject<int>();
                        this.GiftName = this.UserGuardLevel == 3 ? "舰长" : this.UserGuardLevel == 2 ? "提督" : this.UserGuardLevel == 1 ? "总督" : "";
                        this.GiftCount = obj["data"]["num"].ToObject<int>();
                        break;
                    }
                case "SUPER_CHAT_MESSAGE":
                    {
                        this.MsgType = MsgTypeEnum.SuperChat;
                        this.CommentText = obj["data"]["message"]?.ToString();
                        this.UserID = obj["data"]["uid"].ToObject<int>();
                        this.UserName = obj["data"]["user_info"]["uname"].ToString();
                        this.Price = obj["data"]["price"].ToObject<double>();
                        this.SCKeepTime = obj["data"]["time"].ToObject<int>();
                        break;
                    }
                case "ROOM_CHANGE":
                    {
                        this.MsgType = MsgTypeEnum.RoomChange;
                        this.Title = obj["data"]?["title"]?.ToObject<string>();
                        break;
                    }
                /*
                case "WELCOME":
                    {
                        MsgType = MsgTypeEnum.Welcome;
                        UserName = obj["data"]["uname"].ToObject<string>();
                        UserID = obj["data"]["uid"].ToObject<int>();
                        IsVIP = true;
                        IsAdmin = obj["data"]?["is_admin"]?.ToObject<bool>() ?? obj["data"]?["isadmin"]?.ToObject<string>() == "1";
                        break;
                    }
                case "WELCOME_GUARD":
                    {
                        MsgType = MsgTypeEnum.WelcomeGuard;
                        UserName = obj["data"]["username"].ToObject<string>();
                        UserID = obj["data"]["uid"].ToObject<int>();
                        UserGuardLevel = obj["data"]["guard_level"].ToObject<int>();
                        break;
                    }
                */
                default:
                    {
                        this.MsgType = MsgTypeEnum.Unknown;
                        break;
                    }
            }
        }
    }
}
