using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using NLog;
using System;
using System.Collections.Generic;

namespace BililiveRecorder.Core
{
    public class DanmakuRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public List<MsgTypeEnum> record_filter;
        private StreamMonitor _monitor;
        int roomId = 0;
        IRecordedRoom _recordedRoom;

        private bool isRecording = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monitor">对应房间的监视器</param>
        /// <param name="config">设置</param>
        public DanmakuRecorder(StreamMonitor monitor, ConfigV1 config, IRecordedRoom recordedRoom)
        {
            logger.Log(LogLevel.Debug, "弹幕录制模块已装载");
            record_filter = new List<MsgTypeEnum>();
            if (config.RecDanmaku) record_filter.Add(MsgTypeEnum.Comment);
            if (config.RecDanmaku_gift) record_filter.Add(MsgTypeEnum.GiftSend);
            if (config.RecDanmaku_guardbuy) record_filter.Add(MsgTypeEnum.GuardBuy);
            if (config.RecDanmaku_unknown) record_filter.Add(MsgTypeEnum.Unknown);
            if (config.RecDanmaku_welguard) record_filter.Add(MsgTypeEnum.WelcomeGuard);
            if (config.RecDanmaku_welcome) record_filter.Add(MsgTypeEnum.Welcome);

            _recordedRoom = recordedRoom;
            roomId = recordedRoom.RoomId;
            _monitor = monitor;
            monitor.StreamStarted += _StreamStarted;
            monitor.ReceivedDanmaku += Receiver_ReceivedDanmaku;
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            logger.Log(LogLevel.Debug, "收到一条弹幕；" + _recordedRoom.IsRecording);
            if (_recordedRoom.IsRecording && record_filter.Contains(e.Danmaku.MsgType))//正在录制符合要记录的类型
            {
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

        }
    }
}
