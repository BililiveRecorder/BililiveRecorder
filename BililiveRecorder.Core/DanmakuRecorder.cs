using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace BililiveRecorder.Core
{
    public class DanmakuRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public List<MsgTypeEnum> record_filter;
        private static Dictionary<int,DanmakuRecorder> _list = new Dictionary<int, DanmakuRecorder>();
        private StreamMonitor _monitor;
        int roomId = 0;
        RecordedRoom _recordedRoom;
        StreamWriter stream_to_file;
        /// <summary>
        /// 注意！这个变量没有后缀的
        /// </summary>
        string using_fname;

        int stream_begin;

        //private bool isRecording = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monitor">对应房间的监视器</param>
        /// <param name="config">设置</param>
        public DanmakuRecorder(StreamMonitor monitor, ConfigV1 config, RecordedRoom recordedRoom)
        {
            //recordedRoom.rec_path
            using_fname = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds.ToString();
            stream_to_file = new StreamWriter(using_fname + ".xml");
            logger.Log(LogLevel.Debug, "弹幕录制暂存为:" + using_fname);
            record_filter = new List<MsgTypeEnum>();
            
            if (config.RecDanmaku) record_filter.Add(MsgTypeEnum.Comment);
            if (config.RecDanmaku_gift) record_filter.Add(MsgTypeEnum.GiftSend);
            if (config.RecDanmaku_guardbuy) record_filter.Add(MsgTypeEnum.GuardBuy);
            if (config.RecDanmaku_unknown) record_filter.Add(MsgTypeEnum.Unknown);
            if (config.RecDanmaku_welguard) record_filter.Add(MsgTypeEnum.WelcomeGuard);
            if (config.RecDanmaku_welcome) record_filter.Add(MsgTypeEnum.Welcome);

            #region 弹幕文件的头部
            stream_to_file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF - 8\"?>");
            stream_to_file.WriteLine("<!-- " +
                "BililiveRecorder\n" +
                "文件中将包含一些必要的冗余信息以便在时间轴错乱时有机会重新校对时间轴\n" +
                "这些冗余信息不能被其余的弹幕查看软件所理解\n" +
                "如果这个文件无法被正确使用，而里面记录了您重要的录播等弹幕数据，请联系我；\n" +
                "如果你相信软件存在问题，欢迎创建Issue\n\n" +
                "[弹幕部分开发者]\n" +
                "Github: @developer_ken\n" +
                "E-mail: dengbw01@outlook.com\n" +
                "Bilibili: @鸡生蛋蛋生鸡鸡生万物\n" +
                "QQ: 1250542735\n" +
                " -->");
            stream_to_file.WriteLine("<i>");
            stream_to_file.WriteLine("<chatserver>chat.bilibili.com</chatserver>");
            stream_to_file.WriteLine("<chatid>000" + roomId + "</chatid>");//用000开头表示直播弹幕
            stream_to_file.WriteLine("<mission>0</mission>");
            stream_to_file.WriteLine("<maxlimit>2147483647</maxlimit>");
            stream_to_file.WriteLine("<state>0</state>");
            stream_to_file.WriteLine("<real_name>0</real_name>");
            stream_to_file.WriteLine("<source>k-v</source>");
            #endregion

            _recordedRoom = recordedRoom;
            roomId = recordedRoom.RoomId;
            _monitor = monitor;
            //monitor.StreamStarted += _StreamStarted;
            monitor.ReceivedDanmaku += Receiver_ReceivedDanmaku;
            _list.Add(roomId, this);

            stream_begin = DateTimeToUnixTime(DateTime.Now);
            stream_to_file.WriteLine("<RECOVER_INFO Time_Start=" + stream_begin + " />");
            logger.Log(roomId, LogLevel.Debug, "弹幕录制：直播间开播(@" + stream_begin + ")");
        }
        public static int DateTimeToUnixTime(DateTime dateTime)
        {
            return (int)(dateTime - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            //logger.Log(LogLevel.Debug, "收到一条弹幕；" + e.Danmaku.RawData);
            if (_recordedRoom.IsRecording && record_filter.Contains(e.Danmaku.MsgType))//正在录制符合要记录的类型
            {

                //TODO: 从Json中拿出发送时间戳和其他信息并转存为某一格式
                //<d p="time, type, fontsize, color, timestamp, pool, userID, danmuID">TEXT</d>
                switch (e.Danmaku.MsgType)
                {
                    case MsgTypeEnum.Comment:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">" + e.Danmaku.CommentText);
                        string[] displaydata_ = e.Danmaku.DanmakuDisplayInfo.ToString()
                            .Replace("[","").Replace("]", "").Replace("\r", "").Replace("\n", "").Replace(" ", "").Split(',');
                        //logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">SENDTIME = " + e.Danmaku.SendTime);
                        StringBuilder sb = new StringBuilder(70);
                        displaydata_[0] = (e.Danmaku.SendTime - stream_begin).ToString();
                        displaydata_[6] = e.Danmaku.UserID.ToString();
                        displaydata_[7] = displaydata_[7].Replace("\"", "");
                        foreach (string arg in displaydata_)
                        {
                            sb.Append(arg + ",");
                        }
                        sb.Remove(sb.Length-1, 1);
                        logger.Log(LogLevel.Debug, "[弹幕]" + sb);
                        stream_to_file.WriteLine("<d p=\"" + sb + "\" recover_info_sendtime=" + e.Danmaku.SendTime + ">" + e.Danmaku.CommentText + "</d>");
                        break;
                    case MsgTypeEnum.GiftSend:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">(" + e.Danmaku.GiftName + ") * " + e.Danmaku.GiftCount);
                        break;
                    case MsgTypeEnum.GuardBuy:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">(上船)" + e.Danmaku.GiftCount + "月");
                        break;
                    case MsgTypeEnum.Welcome:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">(欢迎老爷)");
                        break;
                    case MsgTypeEnum.WelcomeGuard:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">(欢迎船员)");
                        break;
                    case MsgTypeEnum.Unknown:
                        logger.Log(LogLevel.Debug, "[弹幕](未解析)" + e.Danmaku.RawData);
                        break;
                    default:
                        break;
                }
            }
        }

        public static DanmakuRecorder getRecorderbyRoomId(int roomid)
        {
            return _list[roomid];
        }

        public void FinishFile()
        {
            try
            {
                stream_to_file.WriteLine("</i>");
                stream_to_file.Flush();
                stream_to_file.Close();
                File.Move(using_fname + ".xml", _recordedRoom.rec_path + ".xml");
                logger.Log(LogLevel.Debug, "弹幕文件已保存到：" + _recordedRoom.rec_path + ".xml");
                _list.Remove(roomId);
            }
            catch(Exception err) {
                logger.Log(LogLevel.Error, err.Message);
            }
        }
    }
}
