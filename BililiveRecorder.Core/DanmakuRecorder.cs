using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BililiveRecorder.Core
{
    public class DanmakuRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public List<MsgTypeEnum> record_filter;
        private StreamMonitor _monitor;
        int roomId = 0;
        RecordedRoom _recordedRoom;
        StreamWriter stream_to_file;

        //private bool isRecording = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monitor">对应房间的监视器</param>
        /// <param name="config">设置</param>
        public DanmakuRecorder(StreamMonitor monitor, ConfigV1 config, RecordedRoom recordedRoom)
        {
            stream_to_file = new StreamWriter(recordedRoom.rec_path.Replace(".flv",".xml"));
            logger.Log(LogLevel.Debug, "弹幕录制模块已装载");
            record_filter = new List<MsgTypeEnum>();
            
            if (config.RecDanmaku) record_filter.Add(MsgTypeEnum.Comment);
            if (config.RecDanmaku_gift) record_filter.Add(MsgTypeEnum.GiftSend);
            if (config.RecDanmaku_guardbuy) record_filter.Add(MsgTypeEnum.GuardBuy);
            if (config.RecDanmaku_unknown) record_filter.Add(MsgTypeEnum.Unknown);
            if (config.RecDanmaku_welguard) record_filter.Add(MsgTypeEnum.WelcomeGuard);
            if (config.RecDanmaku_welcome) record_filter.Add(MsgTypeEnum.Welcome);

            #region 弹幕文件的头部
            stream_to_file.WriteLine("<?xml version=\"1.0\" encoding=\"UTF - 8\"?>");
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
            monitor.StreamStarted += _StreamStarted;
            monitor.ReceivedDanmaku += Receiver_ReceivedDanmaku;
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (_recordedRoom.IsRecording && record_filter.Contains(e.Danmaku.MsgType))//正在录制符合要记录的类型
            {
                //logger.Log(LogLevel.Debug, "收到一条弹幕；" + e.Danmaku.RawData);

                //TODO: 从Json中拿出发送时间戳和其他信息并转存为某一格式
                //<d p="time, type, fontsize, color, timestamp, pool, userID, danmuID">TEXT</d>
                logger.Log(LogLevel.Debug, "[弹幕]" + e.Danmaku.RawData);
                switch (e.Danmaku.MsgType)
                {
                    case MsgTypeEnum.Comment:
                        logger.Log(LogLevel.Info, "[弹幕]<" + e.Danmaku.UserName + ">" + e.Danmaku.CommentText);
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
                        logger.Log(LogLevel.Warn, "[弹幕](未解析)" + e.Danmaku.RawData);
                        break;
                    default:
                        break;
                }
            }
        }

        private void _StreamStarted(object sender, StreamStartedArgs e)
        {
            //logger.Log(LogLevel.Debug, "弹幕录制：直播间开播");
        }
    }
}
