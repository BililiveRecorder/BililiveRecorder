using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V2;
using Newtonsoft.Json;
using NLog;
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

        private readonly RoomConfig roomConfig;
        private readonly BililiveAPI bililiveAPI;

        private Exception dmError = null;
        private TcpClient dmClient;
        private NetworkStream dmNetStream;
        private Thread dmReceiveMessageLoopThread;
        private readonly CancellationTokenSource dmTokenSource = null;
        private bool dmConnectionTriggered = false;
        private readonly Timer httpTimer;

        private int RoomId { get => this.roomConfig.RoomId; set => this.roomConfig.RoomId = value; }

        public bool IsMonitoring { get; private set; } = false;
        public bool IsDanmakuConnected => this.dmClient?.Connected ?? false;
        public event RoomInfoUpdatedEvent RoomInfoUpdated;
        public event StreamStartedEvent StreamStarted;
        public event ReceivedDanmakuEvt ReceivedDanmaku;
        public event PropertyChangedEventHandler PropertyChanged;

        public StreamMonitor(RoomConfig roomConfig, BililiveAPI bililiveAPI)
        {
            this.roomConfig = roomConfig;
            this.bililiveAPI = bililiveAPI;

            ReceivedDanmaku += this.Receiver_ReceivedDanmaku;
            RoomInfoUpdated += this.StreamMonitor_RoomInfoUpdated;

            this.dmTokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(30), () =>
            {
                if (this.dmNetStream != null && this.dmNetStream.CanWrite)
                {
                    try
                    {
                        this.SendSocketData(2);
                    }
                    catch (Exception) { }
                }
            }, this.dmTokenSource.Token);

            this.httpTimer = new Timer(roomConfig.TimingCheckInterval * 1000)
            {
                Enabled = false,
                AutoReset = true,
                SynchronizingObject = null,
                Site = null
            };
            this.httpTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    this.Check(TriggerType.HttpApi);
                }
                catch (Exception ex)
                {
                    logger.Log(this.RoomId, LogLevel.Warn, "获取直播间开播状态出错", ex);
                }
            };

            roomConfig.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName.Equals(nameof(roomConfig.TimingCheckInterval)))
                {
                    this.httpTimer.Interval = roomConfig.TimingCheckInterval * 1000;
                }
            };
        }

        private void StreamMonitor_RoomInfoUpdated(object sender, RoomInfoUpdatedArgs e)
        {
            this.RoomId = e.RoomInfo.RoomId;
            // TODO: RecordedRoom 里的 RoomInfoUpdated Handler 也会设置一次 RoomId
            // 暂时保持不变，此处需要使用请求返回的房间号连接弹幕服务器
            if (!this.dmConnectionTriggered)
            {
                this.dmConnectionTriggered = true;
                Task.Run(() => this.ConnectWithRetryAsync());
            }
        }

        private void Receiver_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            switch (e.Danmaku.MsgType)
            {
                case MsgTypeEnum.LiveStart:
                    if (this.IsMonitoring)
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
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(StreamMonitor));
            }

            this.IsMonitoring = true;
            this.httpTimer.Start();
            this.Check(TriggerType.HttpApi);
            return true;
        }

        public void Stop()
        {
            if (this.disposedValue)
            {
                throw new ObjectDisposedException(nameof(StreamMonitor));
            }

            this.IsMonitoring = false;
            this.httpTimer.Stop();
        }

        public void Check(TriggerType type, int millisecondsDelay = 0)
        {
            if (this.disposedValue)
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
                if ((await this.FetchRoomInfoAsync().ConfigureAwait(false))?.IsStreaming ?? false)
                {
                    StreamStarted?.Invoke(this, new StreamStartedArgs() { type = type });
                }
            });
        }

        public async Task<RoomInfo> FetchRoomInfoAsync()
        {
            RoomInfo roomInfo = await this.bililiveAPI.GetRoomInfoAsync(this.RoomId).ConfigureAwait(false);
            if (roomInfo != null)
                RoomInfoUpdated?.Invoke(this, new RoomInfoUpdatedArgs { RoomInfo = roomInfo });
            return roomInfo;
        }

        #endregion
        #region 弹幕连接

        private async Task ConnectWithRetryAsync()
        {
            bool connect_result = false;
            while (!this.IsDanmakuConnected && !this.dmTokenSource.Token.IsCancellationRequested)
            {
                logger.Log(this.RoomId, LogLevel.Info, "连接弹幕服务器...");
                connect_result = await this.ConnectAsync().ConfigureAwait(false);
                if (!connect_result)
                    await Task.Delay((int)Math.Max(this.roomConfig.TimingDanmakuRetry, 0));
            }

            if (connect_result)
            {
                logger.Log(this.RoomId, LogLevel.Info, "弹幕服务器连接成功");
            }
        }

        private async Task<bool> ConnectAsync()
        {
            if (this.IsDanmakuConnected) { return true; }

            try
            {
                var (token, host, port) = await this.bililiveAPI.GetDanmuConf(this.RoomId);

                logger.Log(this.RoomId, LogLevel.Debug, $"连接弹幕服务器 {host}:{port} {(string.IsNullOrWhiteSpace(token) ? "无" : "有")} token");

                this.dmClient = new TcpClient();
                await this.dmClient.ConnectAsync(host, port).ConfigureAwait(false);
                this.dmNetStream = this.dmClient.GetStream();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDanmakuConnected)));

                this.dmReceiveMessageLoopThread = new Thread(this.ReceiveMessageLoop)
                {
                    Name = "ReceiveMessageLoop " + this.RoomId,
                    IsBackground = true
                };
                this.dmReceiveMessageLoopThread.Start();

                var hello = JsonConvert.SerializeObject(new
                {
                    uid = 0,
                    roomid = this.RoomId,
                    protover = 2,
                    platform = "web",
                    clientver = "1.11.0",
                    type = 2,
                    key = token,

                }, Formatting.None);
                this.SendSocketData(7, hello);
                this.SendSocketData(2);

                return true;
            }
            catch (Exception ex)
            {
                this.dmError = ex;
                logger.Log(this.RoomId, LogLevel.Warn, "连接弹幕服务器错误", ex);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDanmakuConnected)));
                return false;
            }
        }

        private void ReceiveMessageLoop()
        {
            logger.Log(this.RoomId, LogLevel.Trace, "ReceiveMessageLoop Started");
            try
            {
                var stableBuffer = new byte[16];
                var buffer = new byte[4096];
                while (this.IsDanmakuConnected)
                {
                    this.dmNetStream.ReadB(stableBuffer, 0, 16);
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

                    this.dmNetStream.ReadB(buffer, 0, payloadlength);

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
                                    logger.Log(this.RoomId, LogLevel.Warn, "", ex);
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
                this.dmError = ex;
                // logger.Error(ex);

                logger.Log(this.RoomId, LogLevel.Debug, "Disconnected");
                this.dmClient?.Close();
                this.dmNetStream = null;
                if (!(this.dmTokenSource?.IsCancellationRequested ?? true))
                {
                    logger.Log(this.RoomId, LogLevel.Warn, "弹幕连接被断开，将尝试重连", ex);
                    Task.Run(async () =>
                    {
                        await Task.Delay((int)Math.Max(this.roomConfig.TimingDanmakuRetry, 0));
                        await this.ConnectWithRetryAsync();
                    });
                }
            }
            finally
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsDanmakuConnected)));
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
                this.dmNetStream.Write(buffer, 0, buffer.Length);
                this.dmNetStream.Flush();
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
                this.PacketLength = IPAddress.HostToNetworkOrder(this.PacketLength);
                this.HeaderLength = IPAddress.HostToNetworkOrder(this.HeaderLength);
                this.Version = IPAddress.HostToNetworkOrder(this.Version);
                this.Action = IPAddress.HostToNetworkOrder(this.Action);
                this.Parameter = IPAddress.HostToNetworkOrder(this.Parameter);
            }
        }


        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.dmTokenSource?.Cancel();
                    this.dmTokenSource?.Dispose();
                    this.httpTimer?.Dispose();
                    this.dmClient?.Close();
                }

                this.dmNetStream = null;
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            this.Dispose(true);
        }
        #endregion
    }
}
