using BililiveRecorder.Core.Config;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            Task.Run(() => ConnectWithRetryAsync());
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            switch (e.Danmaku.MsgType)
            {
                case MsgTypeEnum.LiveStart:
                    if (IsMonitoring)
                    {
                        Task.Run(() => StreamStarted?.Invoke(this, new StreamStartedArgs() { type = TriggerType.Danmaku }));
                    }
                    break;
                case MsgTypeEnum.LiveEnd:
                    break;
                default:
                    break;
            }
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
            httpTimer.Stop();
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

        private async Task ConnectWithRetryAsync()
        {
            bool connect_result = false;
            while (!dmTcpConnected && !dmTokenSource.Token.IsCancellationRequested)
            {
                Thread.Sleep((int)Math.Max(config.TimingDanmakuRetry, 0));
                logger.Log(Roomid, LogLevel.Info, "连接弹幕服务器...");
                connect_result = await ConnectAsync().ConfigureAwait(false);
            }

            if (connect_result)
            {
                logger.Log(Roomid, LogLevel.Info, "弹幕服务器连接成功");
            }
        }

        private async Task<bool> ConnectAsync()
        {
            if (dmTcpConnected) { return true; }

            try
            {
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
        #endregion
    }
}
