using BililiveRecorder.Core.Config;
using BililiveRecorder.FlvProcessor;
using Newtonsoft.Json;
using NLog;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core
{
    /**
     * 直播状态监控
     * 分为弹幕连接和HTTP轮询两部分
     * 
     * 弹幕连接：
     * 一直保持连接，并把收到的弹幕保存到数据库
     * 
     * HTTP轮询：
     * 只有在监控启动时运行，根据直播状态触发事件
     * 
     * 
     * */
    public class StreamMonitor : IStreamMonitor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Func<TcpClient> funcTcpClient;
        private readonly ConfigV1 config;

#pragma warning disable IDE1006 // 命名样式
        private bool dmTcpConnected => dmClient?.Connected ?? false;
#pragma warning restore IDE1006 // 命名样式
        private Exception dmError = null;
        private TcpClient dmClient;
        private NetworkStream dmNetStream;
        private Thread dmReceiveMessageLoopThread;
        private CancellationTokenSource dmTokenSource = null;
        private readonly Timer httpTimer;

        public int Roomid { get; private set; } = 0;
        public string StreamerName { get; set; } = "...";
        public DateTime Time { get; set; }
        public string path { get; set; }
        public string Title { get; set; }
        public bool IsRecording { get; set; }
        public bool IsMonitoring { get; private set; } = false;

        public event RoomInfoUpdatedEvent RoomInfoUpdated;
        public event StreamStartedEvent StreamStarted;
        public event ReceivedDanmakuEvt ReceivedDanmaku;

        public StreamMonitor(int roomid, Func<TcpClient> funcTcpClient, ConfigV1 config)
        {
            this.funcTcpClient = funcTcpClient;
            this.config = config;
            Roomid = roomid;
            ReceivedDanmaku += Receiver_ReceivedDanmaku;

            dmTokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(30), () =>
            {
                if (dmNetStream != null && dmNetStream.CanWrite)
                {
                    try
                    {
                        SendSocketData(2);
                    }
                    catch (Exception) { }
                }
            }, dmTokenSource.Token);

            httpTimer = new Timer(config.TimingCheckInterval * 1000)
            {
                Enabled = false,
                AutoReset = true,
                SynchronizingObject = null,
                Site = null
            };
            httpTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    Check(TriggerType.HttpApi);
                }
                catch (Exception ex)
                {
                    logger.Log(Roomid, LogLevel.Warn, "获取直播间开播状态出错", ex);
                }
            };

            config.PropertyChanged += (sender, e) =>
                    {
                        if (e.PropertyName.Equals(nameof(config.TimingCheckInterval)))
                        {
                            httpTimer.Interval = config.TimingCheckInterval * 1000;
                        }
                    };
            firstConnent = true;
            Task.Run(() => ConnectWithRetryAsync());
        }
        //private List<danmuinMem> danmuMem;
        private List<danmutoCal> danmu = new List<danmutoCal>();
        private List<danmutoCal> liwu = new List<danmutoCal>();
        private List<danmutoCal> SC = new List<danmutoCal>();
        private List<DanmakuModel> danmus = new List<DanmakuModel>();

        private int danmuQueue;
        private int fontsize;
        private int liwufontsize;
        private double flyspeed;
        private string readytoWrite;
        //private string saveinMem;
        private bool isCaling;
        private bool isWriting;
        private async void resetDanmuQueue(int danmuQueueNow, string filename)
        {
            await Task.Delay(200);
            if (danmuQueueNow == danmuQueue && danmuQueue != 0)
            {
                if (isCaling)
                {
                    return;
                }
                isCaling = true;
                LogDanmu(danmuQueue, new List<DanmakuModel>(danmus));
                if (isWriting)
                {
                    return;
                }
                isWriting = true;
                if (readytoWrite != null && readytoWrite != "")
                {
                    using (var outfile = new StreamWriter(filename, true))
                    {
                        outfile.WriteLine(readytoWrite.Remove(readytoWrite.Length - 2));
                    }
                    readytoWrite = null;
                }
                isWriting = false;
                isCaling = false;
            }
        }

        private void LogDanmu(int danmuQueue, List<DanmakuModel> danmustemp)
        {
            string LinetoWritten;
            DateTime now = Time;
            int leftCount = danmuQueue;
            foreach (DanmakuModel danmu in danmustemp)
            {
                //logger.Log(LogLevel.Debug, "log");
                if (leftCount != 0) leftCount--;
                if ((LinetoWritten = convert(danmu, leftCount, now)) != "")
                {
                    readytoWrite += LinetoWritten + "\r\n";
                }

            }
            danmustemp.Clear();
            if (danmus.Count != 0) danmus.RemoveRange(0, danmuQueue);
            this.danmuQueue -= danmuQueue;

        }
        private string convert(DanmakuModel danmaku, int leftCount, DateTime now)
        {
            TimeSpan timeDiff = DateTime.Now - now;
            int startTimePre = (int)((timeDiff.TotalMilliseconds - 1000.0 / danmuQueue * leftCount + 1000) / 10.0);
            int startTimeHour = startTimePre / 100 / 3600;
            int startTimeMin = startTimePre / 100 / 60 % 60;
            int startTimeSec = startTimePre / 100 % 60;
            int startTimeMs = startTimePre % 100;
            string startTime = startTimeHour.ToString() + ':' +
                               startTimeMin.ToString().PadLeft(2, '0') + ':' +
                               startTimeSec.ToString().PadLeft(2, '0') + '.' +
                               startTimeMs.ToString().PadLeft(2, '0');//获取字幕开始时间

            string result;
            switch (danmaku.MsgType)
            {
                case MsgTypeEnum.Comment:
                    {
                        string color = danmaku.CommentColor.Substring(4, 2) + danmaku.CommentColor.Substring(2, 2) + danmaku.CommentColor.Substring(0, 2);
                        string name = danmaku.UserName;
                        string text = danmaku.CommentText;
                        int danmuBasicFlyTime = 15000;
                        int danmuLength = danmaku.CommentText.Length;
                        int danmuDiffFlyTime = (int)(Math.Log10(danmuLength) * 2.33 * 1000.0);
                        int danmuFlyTime = (int)((danmuBasicFlyTime - danmuDiffFlyTime) / (((fontsize - 26) * 0.0125 + 1) * flyspeed));
                        int endTimePre = startTimePre + danmuFlyTime / 10;
                        int endTimeHour = endTimePre / 100 / 3600;
                        int endTimeMin = endTimePre / 100 / 60 % 60;
                        int endTimeSec = endTimePre / 100 % 60;
                        int endTimeMs = endTimePre % 100;
                        string endTime = endTimeHour.ToString() + ':' +
                                         endTimeMin.ToString().PadLeft(2, '0') + ':' +
                                         endTimeSec.ToString().PadLeft(2, '0') + '.' +
                                         endTimeMs.ToString().PadLeft(2, '0');//获取字幕结束时间
                        danmu.Add(new danmutoCal
                        {
                            Length = danmuLength,
                            StartTime = startTimePre,
                            EndTime = endTimePre,
                            FlyTime = danmuFlyTime / 10,
                            FlySpeed = (1920 + danmuLength * (double)fontsize) / (endTimePre - startTimePre),
                            Pos = 0
                        });
                        for (int i = 0; i < danmu.Count; i++)
                        {
                            if (danmu[i] != null && danmu[danmu.Count - 1] != null)
                            {
                                if (danmu[i].EndTime < danmu[danmu.Count - 1].StartTime)
                                {
                                    danmu.RemoveAt(i);
                                }
                            }
                        }
                        if (danmu.Count > 1)
                            CalPos(danmu[danmu.Count - 1]);
                        double pos = danmu[danmu.Count - 1].Pos;
                        int danmuXend = 0 - danmuLength * (int)fontsize;
                        int danmuY = (int)(pos * (fontsize * 1.0 + 4.0) + 1);
                        string effect = "{\\be" + Math.Round(fontsize / 26.0, 2) + "\\move(1920," + danmuY + "," + danmuXend + "," + danmuY + ")" + (color == "FFFFFF" ? "" : "\\c" + color) + "}";
                        result = "Dialogue: 0," + startTime + "," + endTime + ",danmu,,0,0,0," + name + "," + effect + text;
                        break;
                    }
                case MsgTypeEnum.GiftSend:
                case MsgTypeEnum.GuardBuy:
                    {
                        string name = danmaku.UserName;//获取赠送人
                        string text = name + " 赠送了 " + danmaku.GiftName + " x " + danmaku.GiftCount;//获取赠送信息
                        int endTimePre = startTimePre + 300;
                        int endTimeHour = endTimePre / 100 / 3600;
                        int endTimeMin = (endTimePre / 100 / 60) % 60;
                        int endTimeSec = (endTimePre / 100) % 60;
                        int endTimeMs = endTimePre % 100;
                        string endTime = endTimeHour.ToString() + ':' +
                                           endTimeMin.ToString().PadLeft(2, '0') + ':' +
                                           endTimeSec.ToString().PadLeft(2, '0') + '.' +
                                           endTimeMs.ToString().PadLeft(2, '0');//获取字幕结束时间

                        liwu.Add(new danmutoCal
                        {
                            StartTime = startTimePre,
                            EndTime = endTimePre,
                            Pos = 0
                        });
                        for (int i = 0; i < liwu.Count; i++)
                        {
                            if (liwu[i] != null && liwu[liwu.Count - 1] != null)
                            {
                                if (liwu[i].EndTime < liwu[liwu.Count - 1].StartTime)
                                {
                                    liwu.RemoveAt(i);
                                }
                            }
                        }
                        if (liwu.Count > 1)
                            CalLiwuPos(liwu[liwu.Count - 1]);
                        int danmuPos = liwu[liwu.Count - 1].Pos;
                        int danmuY = (int)(1080 - 20 - danmuPos * liwufontsize * 1.2);
                        string effect = "{\\be1\\fad(500,500)\\clip(0,440,1920,1080)\\pos(20," + danmuY + ")}";
                        result = "Dialogue: 1," + startTime + "," + endTime + ",liwu,,0,0,0," + name + "," + effect + text;
                        break;
                    }
                case MsgTypeEnum.SuperChat:
                    {
                        string name = danmaku.UserName + ": ";//获取赠送人
                        string price = "￥" + danmaku.Price + " ";
                        int keepTime = danmaku.SCKeepTime;
                        string commenttext = danmaku.CommentText;
                        string text = price + name + commenttext;//获取赠送信息
                        if (commenttext == "") text.Remove(text.Length - 1);
                        int endTimePre = startTimePre + 1500;
                        int endTimeHour = endTimePre / 100 / 3600;
                        int endTimeMin = (endTimePre / 100 / 60) % 60;
                        int endTimeSec = (endTimePre / 100) % 60;
                        int endTimeMs = endTimePre % 100;
                        string endTime = endTimeHour.ToString() + ':' +
                                           endTimeMin.ToString().PadLeft(2, '0') + ':' +
                                           endTimeSec.ToString().PadLeft(2, '0') + '.' +
                                           endTimeMs.ToString().PadLeft(2, '0');//获取字幕结束时间

                        SC.Add(new danmutoCal
                        {
                            StartTime = startTimePre,
                            EndTime = endTimePre,
                            Pos = 0
                        });
                        for (int i = 0; i < SC.Count; i++)
                        {
                            if (SC[i] != null && SC[SC.Count - 1] != null)
                            {
                                if (SC[i].EndTime < SC[SC.Count - 1].StartTime)
                                {
                                    SC.RemoveAt(i);
                                }
                            }
                        }
                        if (SC.Count > 1)
                            CalSCPos(SC[SC.Count - 1]);
                        int danmuPos = SC[SC.Count - 1].Pos;
                        string UnderlingColor = "FFFFFF";
                        switch (keepTime)
                        {
                            case 60:
                                UnderlingColor = "B05F2E";
                                break;
                            case 120:
                                UnderlingColor = "9E7C46";
                                break;
                            case 300:
                                UnderlingColor = "37B6E0";
                                break;
                            case 1800:
                                UnderlingColor = "4995DD";
                                break;
                            case 3600:
                                UnderlingColor = "5151E0";
                                break;
                            case 7200:
                                UnderlingColor = "3520A7";
                                break;
                            default:
                                UnderlingColor = "FFFFFF";
                                break;
                        }
                        int danmuY = (int)(1080 - 20 - danmuPos * liwufontsize * 1.2);
                        string effect = "{\\an3\\be" + Math.Round(liwufontsize / 22.0, 2) + "\\fad(500,500)\\clip(0,440,1920,1080)\\pos(1900," + danmuY + ")}";
                        result = "Dialogue: 2," + startTime + "," + endTime + ",SC,,0,0,0," + name + "," + effect + text + "\r\n";
                        string countdowneffect = "{\\fad(500,500)\\an3\\move(1900," + danmuY + "," + (1900 + liwufontsize * 8) + "," + danmuY + ")\\3aff\\c" + UnderlingColor + "\\clip(" + (1900 - liwufontsize * 8) + ",0,1900,1080)}";
                        result += "Dialogue: 3," + startTime + "," + endTime + ",SC,,0,0,0,SuperChat," + countdowneffect + "＿＿＿＿＿＿＿＿";
                        break;
                    }
                default:
                    result = "";
                    break;
            }
            return result;

            void CalPos(danmutoCal dm)
            {
                //算法原理：将当前弹幕和前面相同位置的弹幕做位置判断，如不可放，往下移动1行并继续判断，如此循环。
                //int maxPos = 0;
                bool ifSuccess = false;
                int start = 0;
                for (int i = 0; i < danmu.Count; i++)
                {
                    if (danmu[i] != null)
                    {
                        start = i;
                        break;
                    }
                }
                while (!ifSuccess)
                {
                    int i = danmu.Count - 2;
                    for (; i >= start; i--)
                    {
                        if (danmu[i] != null)
                        {
                            if (danmu[i].Pos > dm.Pos)
                            {
                                //if (danmu[i].Pos - maxPos == 1) maxPos = danmu[i].Pos;
                                continue;
                            }
                            if (danmu[i].Pos == dm.Pos)
                            {
                                if (ifCanBePosed(danmu[i], dm))
                                {
                                    return;
                                }
                                else
                                {
                                    dm.Pos++;
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    if (i == start - 1) return;
                }
            }
            void CalLiwuPos(danmutoCal lw)
            {
                //int maxPos = 0;
                bool ifSuccess = false;
                int start = 0;
                for (int i = 0; i < liwu.Count; i++)
                {
                    if (liwu[i] != null)
                    {
                        start = i;
                        break;
                    }
                }
                while (!ifSuccess)
                {
                    int i = liwu.Count - 2;
                    for (; i >= start; i--)
                    {
                        if (liwu[i] != null)
                        {
                            if (liwu[i].Pos > lw.Pos)
                            {
                                //if (liwu[i].Pos - maxPos == 1) maxPos = liwu[i].Pos;
                                continue;
                            }
                            if (liwu[i].Pos == lw.Pos)
                            {
                                if (liwu[i].EndTime < lw.StartTime)
                                {
                                    return;
                                }
                                else
                                {
                                    lw.Pos++;
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    if (i == start - 1) return;
                }
            }
            void CalSCPos(danmutoCal sc)
            {
                //int maxPos = 0;
                bool ifSuccess = false;
                int start = 0;
                for (int i = 0; i < SC.Count; i++)
                {
                    if (SC[i] != null)
                    {
                        start = i;
                        break;
                    }
                }
                while (!ifSuccess)
                {
                    int i = SC.Count - 2;
                    for (; i >= start; i--)
                    {
                        if (SC[i] != null)
                        {
                            if (SC[i].Pos > sc.Pos)
                            {
                                //if (SC[i].Pos - maxPos == 1) maxPos = SC[i].Pos;
                                continue;
                            }
                            if (SC[i].Pos == sc.Pos)
                            {
                                if (SC[i].EndTime < sc.StartTime)
                                {
                                    return;
                                }
                                else
                                {
                                    sc.Pos++;
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                    if (i == start - 1) return;
                }
            }
            bool ifCanBePosed(danmutoCal dm1, danmutoCal dm2)
            {
                //判断方法：dm2飞到头时，dm1有没有消失
                //                                                                                   dm2到头前的时间=1280/dm2的速度
                //                                                                   dm1还剩下的时间=dm2到头前的时间
                //                                         dm1还能飞的距离=dm1的速度*dm1还剩下的时间
                //dm1在dm2到头前飞出的距离=dm1已经飞的距离+dm1还能飞的距离
                //                         dm1已经飞的距离=dm1的速度*dm1已经飞的时间
                //                                                   dm1已经飞的时间=dm2的开始时间-dm1的开始时间
                //原式：dm1.FlySpeed * (dm2.StartTime - dm1.StartTime) + dm1.FlySpeed * (dm1.EndTime - (1280 / dm2.FlySpeed))
                //简化：dm1.FlySpeed * (dm2.StartTime - dm1.StartTime + dm1.EndTime - (1280 / dm2.FlySpeed))

                if (dm1.FlySpeed * (dm2.StartTime - dm1.StartTime) > (dm1.Length * (config.DanmuFontSize + 1) + 4))
                {
                    if ((dm1.FlySpeed * (dm2.StartTime - dm1.StartTime + (1920 / dm2.FlySpeed))) > (1920 + dm1.Length * config.DanmuFontSize))
                        return true;
                    else
                        return false;
                }
                return false;
            }
        }
        /*private void saveClipDanmu(DanmakuModel e)
        {
            DateTime Now = DateTime.Now;
            danmuMem.Add(new danmuinMem
            {
                danmakuModel = e,
                dateTime = DateTime.Now
            });

            for (int i = 0; (Now - danmuMem[i].dateTime).TotalSeconds < config.ClipLengthPast; i++)
            {
                danmuMem.Remove(danmuMem[i]);
            }
        }*/
        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            switch (e.Danmaku.MsgType)
            {
                case MsgTypeEnum.LiveStart:
                    if (IsMonitoring)
                    {
                        Task.Run(() => StreamStarted?.Invoke(this, new StreamStartedArgs() { type = TriggerType.Danmaku }));
                    }
                    IsRecording = true;
                    break;
                case MsgTypeEnum.LiveEnd:
                    break;
                case MsgTypeEnum.Comment:
                case MsgTypeEnum.GiftSend:
                case MsgTypeEnum.GuardBuy:
                case MsgTypeEnum.SuperChat:
                    if (IsRecording)
                    {
                        logging(e.Danmaku);
                    }
                    //saveClipDanmu(e.Danmaku);
                    break;
                default:
                    break;
            }
        }
        private void logging(DanmakuModel e)
        {
            string filename = path.Replace("flv", "ass");
            if (!File.Exists(filename) && (readytoWrite == null || readytoWrite == ""))
            {
                isCaling = false;
                isWriting = false;
                danmu.Clear();
                liwu.Clear();
                SC.Clear();
                danmus.Clear();
                fontsize = (int)Math.Max(config.DanmuFontSize, 1);
                liwufontsize = (int)Math.Max(Math.Round(fontsize / 26.0 * 22.0, 0), 1);
                flyspeed = Math.Max(config.DanmuFlySpeed, 0.01);
                readytoWrite += "[Script Info]\r\nScriptType: v4.00+\r\n" +
                                  "WrapStyle: 0\r\n" +
                                  "ScaledBorderAndShadow: yes\r\n" +
                                  "PlayResX: 1920\r\n" +
                                  "PlayResY: 1080\r\n" +
                                  "\r\n" +
                                  "[V4+ Styles]\r\n" +
                                  "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding  \r\n" +
                                  "Style: danmu,SimHei," + fontsize + ",&H00FFFFFF,&H000000FF,&H40000000,&H00000000,-1,0,0,0,100,100,0,0,1," + Math.Round(fontsize / 26.0, 2) + ",0,7,10,10,10,1\r\n" +
                                  "Style: liwu,SimHei," + liwufontsize + ",&H00FFFFFF,&H000000FF,&H40000000,&H00000000,-1,0,0,0,100,100,0,0,1," + Math.Round(liwufontsize / 22.0, 2) + ",0,1,10,10,10,1\r\n" +
                                  "Style: SC,SimHei," + liwufontsize + ",&H00FFFFFF,&H000000FF,&H40000000,&H00000000,-1,0,0,0,100,100,0,0,1," + Math.Round(liwufontsize / 22.0, 2) + ",0,1,10,10,10,1\r\n" +
                                  "\r\n" +
                                  "[Events]\r\n" +
                                  "Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text\r\n";
                logger.Log(LogLevel.Debug, "保存弹幕文件: " + filename);
            }
            danmus.Add(e);
            danmuQueue++;

            Task.Run(() => resetDanmuQueue(danmuQueue, filename));
            //logger.Log(LogLevel.Debug, "计算" + danmuQueue.ToString());


        }

        #region 对外API

        public bool Start()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(StreamMonitor));
            }

            IsMonitoring = true;
            httpTimer.Start();
            Check(TriggerType.HttpApi);
            return true;
        }

        public void Stop()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(StreamMonitor));
            }

            IsMonitoring = false;
            path = null;
            httpTimer.Stop();
            danmu.Clear();
            danmus.Clear();
            liwu.Clear();
            SC.Clear();
        }

        public void Check(TriggerType type, int millisecondsDelay = 0)
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(nameof(StreamMonitor));
            }

            if (millisecondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(millisecondsDelay), "不能小于0");
            }

            Task.Run(async () =>
            {
                await Task.Delay(millisecondsDelay).ConfigureAwait(false);
                if ((await FetchRoomInfoAsync().ConfigureAwait(false)).IsStreaming)
                {
                    StreamStarted?.Invoke(this, new StreamStartedArgs() { type = type });
                }
            });
        }

        public async Task<RoomInfo> FetchRoomInfoAsync()
        {
            RoomInfo roomInfo = await BililiveAPI.GetRoomInfoAsync(Roomid).ConfigureAwait(false);
            RoomInfoUpdated?.Invoke(this, new RoomInfoUpdatedArgs { RoomInfo = roomInfo });
            return roomInfo;
        }

        #endregion
        #region 弹幕连接
        private bool firstConnent;
        private async Task ConnectWithRetryAsync()
        {
            bool connect_result = false;
            if (firstConnent)
            {
                logger.Log(Roomid, LogLevel.Info, "连接弹幕服务器...");
                connect_result = await ConnectAsync().ConfigureAwait(false);
                firstConnent = false;
            }
            else
            {
                while (!dmTcpConnected && !dmTokenSource.Token.IsCancellationRequested)
                {
                    Thread.Sleep((int)Math.Max(config.TimingDanmakuRetry, 0));
                    logger.Log(Roomid, LogLevel.Info, "连接弹幕服务器...");
                    connect_result = await ConnectAsync().ConfigureAwait(false);
                }
            }
            if (connect_result)
            {
                logger.Log(Roomid, LogLevel.Info, "弹幕服务器连接成功");
            }
        }
        public async Task<bool> ConnectAsync()
        {
            if (dmTcpConnected) { return true; }

            try
            {
                RoomInfo newRoom = await BililiveAPI.GetRoomInfoAsync(Roomid);
                Roomid = newRoom.RoomId;
                var (token, host, port) = await BililiveAPI.GetDanmuConf(Roomid);

                logger.Log(Roomid, LogLevel.Debug, $"连接弹幕服务器 {host}:{port} {(string.IsNullOrWhiteSpace(token) ? "无" : "有")} token");

                dmClient = funcTcpClient();
                dmClient.Connect(host, port);
                dmNetStream = dmClient.GetStream();

                dmReceiveMessageLoopThread = new Thread(ReceiveMessageLoop)
                {
                    Name = "ReceiveMessageLoop " + Roomid,
                    IsBackground = true
                };
                dmReceiveMessageLoopThread.Start();

                var hello = JsonConvert.SerializeObject(new
                {
                    uid = 0,
                    roomid = Roomid,
                    protover = 2,
                    platform = "web",
                    clientver = "1.11.0",
                    type = 2,
                    key = token,

                }, Formatting.None);
                SendSocketData(7, hello);
                SendSocketData(2);

                return true;
            }
            catch (Exception ex)
            {
                dmError = ex;
                logger.Log(Roomid, LogLevel.Error, "连接弹幕服务器错误", ex);

                return false;
            }
        }

        private void ReceiveMessageLoop()
        {
            logger.Log(Roomid, LogLevel.Trace, "ReceiveMessageLoop Started");
            try
            {
                var stableBuffer = new byte[16];
                var buffer = new byte[4096];
                while (dmTcpConnected)
                {
                    dmNetStream.ReadB(stableBuffer, 0, 16);
                    Parse2Protocol(stableBuffer, out DanmakuProtocol protocol);

                    if (protocol.PacketLength < 16)
                    {
                        throw new NotSupportedException("协议失败: (L:" + protocol.PacketLength + ")");
                    }

                    var payloadlength = protocol.PacketLength - 16;
                    if (payloadlength == 0)
                    {
                        continue;//没有内容了
                    }

                    if (buffer.Length < payloadlength) // 不够长再申请
                    {
                        buffer = new byte[payloadlength];
                    }

                    dmNetStream.ReadB(buffer, 0, payloadlength);

                    if (protocol.Version == 2 && protocol.Action == 5) // 处理deflate消息
                    {
                        // Skip 0x78 0xDA
                        using (DeflateStream deflate = new DeflateStream(new MemoryStream(buffer, 2, payloadlength - 2), CompressionMode.Decompress))
                        {
                            while (deflate.Read(stableBuffer, 0, 16) > 0)
                            {
                                Parse2Protocol(stableBuffer, out protocol);
                                payloadlength = protocol.PacketLength - 16;
                                if (payloadlength == 0)
                                {
                                    continue; // 没有内容了
                                }
                                if (buffer.Length < payloadlength) // 不够长再申请
                                {
                                    buffer = new byte[payloadlength];
                                }
                                deflate.Read(buffer, 0, payloadlength);
                                ProcessDanmaku(protocol.Action, buffer, payloadlength);
                            }
                        }
                    }
                    else
                    {
                        ProcessDanmaku(protocol.Action, buffer, payloadlength);
                    }

                    void ProcessDanmaku(int action, byte[] local_buffer, int length)
                    {
                        //logger.Log(Roomid, LogLevel.Debug, "ReceivedDanmaku");
                        switch (action)
                        {
                            case 3:
                                // var viewer = BitConverter.ToUInt32(local_buffer.Take(4).Reverse().ToArray(), 0); //观众人数
                                break;
                            case 5://playerCommand
                                var json = Encoding.UTF8.GetString(local_buffer, 0, length);
                                try
                                {
                                    ReceivedDanmaku?.Invoke(this, new ReceivedDanmakuArgs() { Danmaku = new DanmakuModel(json) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Log(Roomid, LogLevel.Warn, "", ex);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                dmError = ex;
                // logger.Error(ex);

                logger.Log(Roomid, LogLevel.Debug, "Disconnected");
                dmClient?.Close();
                danmu.Clear();
                liwu.Clear();
                SC.Clear();
                dmNetStream = null;
                if (!(dmTokenSource?.IsCancellationRequested ?? true))
                {
                    logger.Log(Roomid, LogLevel.Warn, "弹幕连接被断开，将尝试重连", ex);
                    _ = ConnectWithRetryAsync();
                }
            }
        }

        private void SendSocketData(int action, string body = "")
        {
            const int param = 1;
            const short magic = 16;
            const short ver = 1;

            var playload = Encoding.UTF8.GetBytes(body);
            var buffer = new byte[(playload.Length + 16)];

            using (var ms = new MemoryStream(buffer))
            {
                var b = BitConverter.GetBytes(buffer.Length).ToBE();
                ms.Write(b, 0, 4);
                b = BitConverter.GetBytes(magic).ToBE();
                ms.Write(b, 0, 2);
                b = BitConverter.GetBytes(ver).ToBE();
                ms.Write(b, 0, 2);
                b = BitConverter.GetBytes(action).ToBE();
                ms.Write(b, 0, 4);
                b = BitConverter.GetBytes(param).ToBE();
                ms.Write(b, 0, 4);
                if (playload.Length > 0)
                {
                    ms.Write(playload, 0, playload.Length);
                }
                dmNetStream.Write(buffer, 0, buffer.Length);
                dmNetStream.Flush();
            }
        }

        private static unsafe void Parse2Protocol(byte[] buffer, out DanmakuProtocol protocol)
        {
            fixed (byte* ptr = buffer)
            {
                protocol = *(DanmakuProtocol*)ptr;
            }
            protocol.ChangeEndian();
        }

        private struct DanmakuProtocol
        {
            /// <summary>
            /// 消息总长度 (协议头 + 数据长度)
            /// </summary>
            public int PacketLength;
            /// <summary>
            /// 消息头长度 (固定为16[sizeof(DanmakuProtocol)])
            /// </summary>
            public short HeaderLength;
            /// <summary>
            /// 消息版本号
            /// </summary>
            public short Version;
            /// <summary>
            /// 消息类型
            /// </summary>
            public int Action;
            /// <summary>
            /// 参数, 固定为1
            /// </summary>
            public int Parameter;
            /// <summary>
            /// 转为本机字节序
            /// </summary>
            public void ChangeEndian()
            {
                PacketLength = IPAddress.HostToNetworkOrder(PacketLength);
                HeaderLength = IPAddress.HostToNetworkOrder(HeaderLength);
                Version = IPAddress.HostToNetworkOrder(Version);
                Action = IPAddress.HostToNetworkOrder(Action);
                Parameter = IPAddress.HostToNetworkOrder(Parameter);
            }
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dmTokenSource?.Cancel();
                    dmTokenSource?.Dispose();
                    httpTimer?.Dispose();
                    dmClient?.Close();
                    danmu.Clear();
                    liwu.Clear();
                    SC.Clear();

                }

                dmNetStream = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
        }

        private class danmutoCal
        {
            public int Length { get; set; }
            public int StartTime { get; set; }
            public int EndTime { get; set; }
            public int FlyTime { get; set; }
            public int Pos { get; set; }
            public int Number { get; set; }
            public double FlySpeed { get; set; }
        }
        private class danmuinMem
        {
            public DanmakuModel danmakuModel { set; get; }
            public DateTime dateTime { set; get; }
        }
        #endregion
    }
}
