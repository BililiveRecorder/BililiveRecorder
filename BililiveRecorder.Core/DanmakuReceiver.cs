using NLog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace BililiveRecorder.Core
{
    public class DanmakuReceiver : IDanmakuReceiver
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string defaulthosts = "broadcastlv.chat.bilibili.com";
        private const string CIDInfoUrl = "http://live.bilibili.com/api/player?id=cid:";

        private string ChatHost = defaulthosts;
        private int ChatPort = 2243;

        private readonly Func<TcpClient> funcTcpClient;
        private TcpClient Client;
        private NetworkStream NetStream;

        private Thread ReceiveMessageLoopThread;
        private CancellationTokenSource HeartbeatLoopSource;

        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                _isConnected = value;
                if (!value)
                {
                    HeartbeatLoopSource.Cancel();
                }
            }
        }
        private bool _isConnected = false;

        public DanmakuReceiver(Func<TcpClient> funcTcpClient)
        {
            this.funcTcpClient = funcTcpClient;
        }

        public Exception Error { get; private set; } = null;
        public uint ViewerCount { get; private set; } = 0;

        public event DisconnectEvt Disconnected;
        public event ReceivedRoomCountEvt ReceivedRoomCount;
        public event ReceivedDanmakuEvt ReceivedDanmaku;

        public bool Connect(int roomId)
        {
            try
            {
                if (IsConnected)
                {
                    throw new InvalidOperationException();
                }

                int channelId = roomId;

                try
                {
                    var request2 = WebRequest.Create(CIDInfoUrl + channelId);
                    request2.Timeout = 2000;
                    var response2 = request2.GetResponse();
                    using (var stream = response2.GetResponseStream())
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var text = sr.ReadToEnd();
                            var xml = "<root>" + text + "</root>";
                            XmlDocument doc = new XmlDocument();
                            doc.LoadXml(xml);
                            ChatHost = doc["root"]["dm_server"].InnerText;
                            ChatPort = int.Parse(doc["root"]["dm_port"].InnerText);
                        }
                    }
                }
                catch (WebException ex)
                {
                    HttpWebResponse errorResponse = ex.Response as HttpWebResponse;
                    if (errorResponse?.StatusCode == HttpStatusCode.NotFound)
                    { // 直播间不存在（HTTP 404）
                        logger.Log(roomId, LogLevel.Warn, "该直播间疑似不存在", ex);
                    }
                    else
                    { // B站服务器响应错误
                        logger.Log(roomId, LogLevel.Warn, "B站服务器响应弹幕服务器地址出错", ex);
                    }
                }
                catch (Exception ex)
                { // 其他错误（XML解析错误？）
                    logger.Log(roomId, LogLevel.Warn, "获取弹幕服务器地址时出现未知错误", ex);
                }

                Client = funcTcpClient();
                
                Client.Connect(ChatHost, ChatPort);
                if (!Client.Connected)
                {
                    return false;
                }
                NetStream = Client.GetStream();
                SendSocketData(7, "{\"roomid\":" + channelId + ",\"uid\":0}");
                IsConnected = true;

                ReceiveMessageLoopThread = new Thread(ReceiveMessageLoop)
                {
                    Name = "ReceiveMessageLoop " + channelId,
                    IsBackground = true
                };
                ReceiveMessageLoopThread.Start();

                HeartbeatLoopSource = new CancellationTokenSource();
                Repeat.Interval(TimeSpan.FromSeconds(30), SendHeartbeat, HeartbeatLoopSource.Token);

                // HeartbeatLoopThread = new Thread(this.HeartbeatLoop);
                // HeartbeatLoopThread.IsBackground = true;
                // HeartbeatLoopThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                Error = ex;
                logger.Log(roomId, LogLevel.Error, "连接弹幕服务器时发生了未知错误", ex);
                return false;
            }
        }

        public void Disconnect()
        {
            IsConnected = false;
            try
            {
                Client.Close();
            }
            catch (Exception)
            { }

            NetStream = null;
        }

        private void ReceiveMessageLoop()
        {
            logger.Trace("ReceiveMessageLoop Started!");
            try
            {
                var stableBuffer = new byte[Client.ReceiveBufferSize];
                while (IsConnected)
                {

                    NetStream.ReadB(stableBuffer, 0, 4);
                    var packetlength = BitConverter.ToInt32(stableBuffer, 0);
                    packetlength = IPAddress.NetworkToHostOrder(packetlength);

                    if (packetlength < 16)
                    {
                        throw new NotSupportedException("协议失败: (L:" + packetlength + ")");
                    }

                    NetStream.ReadB(stableBuffer, 0, 2);//magic
                    NetStream.ReadB(stableBuffer, 0, 2);//protocol_version 
                    NetStream.ReadB(stableBuffer, 0, 4);
                    var typeId = BitConverter.ToInt32(stableBuffer, 0);
                    typeId = IPAddress.NetworkToHostOrder(typeId);

                    NetStream.ReadB(stableBuffer, 0, 4);//magic, params?
                    var playloadlength = packetlength - 16;
                    if (playloadlength == 0)
                    {
                        continue;//没有内容了
                    }

                    typeId = typeId - 1;//和反编译的代码对应 
                    var buffer = new byte[playloadlength];
                    NetStream.ReadB(buffer, 0, playloadlength);
                    switch (typeId)
                    {
                        case 0:
                        case 1:
                        case 2:
                            {
                                var viewer = BitConverter.ToUInt32(buffer.Take(4).Reverse().ToArray(), 0); //观众人数
                                ViewerCount = viewer;
                                try
                                {
                                    ReceivedRoomCount?.Invoke(this, new ReceivedRoomCountArgs() { UserCount = viewer });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex);
                                }
                                break;
                            }
                        case 3:
                        case 4://playerCommand
                            {
                                var json = Encoding.UTF8.GetString(buffer, 0, playloadlength);
                                try
                                {
                                    ReceivedDanmaku?.Invoke(this, new ReceivedDanmakuArgs() { Danmaku = new DanmakuModel(json) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex);
                                }
                                break;
                            }
                        case 5://newScrollMessage
                        case 7:
                        case 16:
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsConnected)
                {
                    Error = ex;
                    logger.Error(ex);
                    _disconnect();
                }
            }
        }

        private void _disconnect()
        {
            if (IsConnected)
            {
                logger.Debug("Disconnected");
                IsConnected = false;
                Client.Close();
                NetStream = null;
                try
                {
                    Disconnected?.Invoke(this, new DisconnectEvtArgs() { Error = Error });
                }
                catch (Exception ex)
                {
                    logger.Warn(ex);
                }
            }
        }

        private void SendHeartbeat()
        {
            SendSocketData(2);
            logger.Trace("Message Sent: Heartbeat");
        }

        private void SendSocketData(int action, string body = "")
        {
            SendSocketData(0, 16, /*protocolversion*/1, action, 1, body);
        }

        private void SendSocketData(int packetlength, short magic, short ver, int action, int param = 1, string body = "")
        {
            var playload = Encoding.UTF8.GetBytes(body);
            if (packetlength == 0)
            {
                packetlength = playload.Length + 16;
            }
            var buffer = new byte[packetlength];
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
                NetStream.Write(buffer, 0, buffer.Length);
                NetStream.Flush();
            }
        }
    }
}
