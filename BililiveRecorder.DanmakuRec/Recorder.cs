using BililiveRecorder.Core;
using System;
using System.Collections.Generic;

namespace BililiveRecorder.DanmakuRec
{
    public class Recorder
    {
        public event ReceivedDanmakuEvt ReceivedDanmaku;
        public List<MsgTypeEnum> record_filter;

        /// <summary>
        /// 装载弹幕记录系统
        /// </summary>
        /// <param name="filter">记录哪些类型的弹幕</param>
        public Recorder(List<MsgTypeEnum> filter)
        {
            record_filter = filter;
            ReceivedDanmaku += Receiver_ReceivedDanmaku;
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            if (record_filter.Contains(e.Danmaku.MsgType))//符合要记录的类型
            {

            }
        }
    }
}
