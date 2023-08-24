using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BililiveRecorder.Core.Config;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Serilog;
using Timer = System.Timers.Timer;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuClient : IDanmakuClient, IDisposable
    {
        private readonly ILogger logger;
        private readonly IDanmakuServerApiClient apiClient;
        private readonly Timer timer;
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        private IDanmakuTransport? danmakuTransport;
        private bool disposedValue;

        public bool Connected => this.danmakuTransport != null;

        public event EventHandler<StatusChangedEventArgs>? StatusChanged;
        public event EventHandler<DanmakuReceivedEventArgs>? DanmakuReceived;

        public Func<string, string?>? BeforeHandshake { get; set; } = null;

        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        public DanmakuClient(IDanmakuServerApiClient apiClient, ILogger logger)
        {
            this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            this.logger = logger?.ForContext<DanmakuClient>() ?? throw new ArgumentNullException(nameof(logger));

            this.timer = new Timer(interval: 1000 * 30)
            {
                AutoReset = true,
                Enabled = false
            };
            this.timer.Elapsed += this.SendPingMessageTimerCallback;
        }

        public async Task DisconnectAsync()
        {
            await this.semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                this.danmakuTransport?.Dispose();
                this.danmakuTransport = null;

                this.timer.Stop();
            }
            finally
            {
                this.semaphoreSlim.Release();
            }

            StatusChanged?.Invoke(this, StatusChangedEventArgs.False);
        }

        public async Task ConnectAsync(int roomid, DanmakuTransportMode transportMode, CancellationToken cancellationToken)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(DanmakuClient));

            if (!Enum.IsDefined(typeof(DanmakuTransportMode), transportMode))
                throw new ArgumentOutOfRangeException(nameof(transportMode), transportMode, "Invalid danmaku transport mode.");

            await this.semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (this.danmakuTransport != null)
                    return;

                var serverInfo = await this.apiClient.GetDanmakuServerAsync(roomid).ConfigureAwait(false);
                if (serverInfo.Data is null)
                    return;

                var danmakuServerInfo = serverInfo.Data.SelectDanmakuServer(transportMode);

                this.logger.Debug("连接弹幕服务器 {Mode} {Host}:{Port} 房间: {RoomId} TokenLength: {TokenLength}", danmakuServerInfo.TransportMode, danmakuServerInfo.Host, danmakuServerInfo.Port, roomid, danmakuServerInfo.Token?.Length);

                IDanmakuTransport transport = danmakuServerInfo.TransportMode switch
                {
                    DanmakuTransportMode.Tcp => new DanmakuTransportTcp(),
                    DanmakuTransportMode.Ws => new DanmakuTransportWebSocket(),
                    DanmakuTransportMode.Wss => new DanmakuTransportSecureWebSocket(),
                    _ => throw new ArgumentOutOfRangeException(nameof(transportMode), transportMode, "Invalid danmaku transport mode."),
                };

                var reader = await transport.ConnectAsync(danmakuServerInfo.Host, danmakuServerInfo.Port, cancellationToken).ConfigureAwait(false);

                this.danmakuTransport = transport;

                await this.SendHelloAsync(roomid, this.apiClient.GetUid(), this.apiClient.GetBuvid3(), danmakuServerInfo.Token ?? string.Empty).ConfigureAwait(false);
                await this.SendPingAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    this.danmakuTransport.Dispose();
                    this.danmakuTransport = null;
                    return;
                }

                this.timer.Start();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessDataAsync(reader, this.ProcessCommand).ConfigureAwait(false);
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception ex)
                    {
                        this.logger.Debug(ex, "Error running ProcessDataAsync");
                    }

                    try
                    {
                        await this.DisconnectAsync().ConfigureAwait(false);
                    }
                    catch (Exception) { }
                });
            }
            finally
            {
                this.semaphoreSlim.Release();
            }

            StatusChanged?.Invoke(this, StatusChangedEventArgs.True);
        }

        private void ProcessCommand(string json)
        {
            try
            {
                var d = new DanmakuModel(json);
                DanmakuReceived?.Invoke(this, new DanmakuReceivedEventArgs(d));
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Error running ProcessCommand");
            }
        }

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void SendPingMessageTimerCallback(object? sender, ElapsedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                await this.semaphoreSlim.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (this.danmakuTransport is null)
                        return;

                    await this.SendPingAsync().ConfigureAwait(false);
                }
                finally
                {
                    this.semaphoreSlim.Release();
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Error running SendPingMessageTimerCallback");
            }
        }

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.timer.Dispose();
                    this.danmakuTransport?.Dispose();
                    this.semaphoreSlim.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DanmakuClient()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Send

        private Task SendHelloAsync(int roomid, long uid, string? buvid, string token)
        {
            var body = JsonConvert.SerializeObject(new
            {
                uid,
                roomid,
                protover = 0,
                buvid,
                platform = "web",
                type = 2,
                key = token,
            }, Formatting.None, jsonSerializerSettings);

            if (this.BeforeHandshake is { } func)
            {
                var newBody = func(body);
                if (newBody is not null)
                {
                    this.logger.Debug("Danmaku BeforeHandshake: {OldBody} => {NewBody}", body, newBody);
                    body = newBody;
                }
            }

            return this.SendMessageAsync(7, body);
        }

        private Task SendPingAsync() => this.SendMessageAsync(2);

        private async Task SendMessageAsync(int action, string body = "")
        {
            if (this.danmakuTransport is not { } transport)
                return;

            var payload = Encoding.UTF8.GetBytes(body);
            const int headerLength = 16;
            var totalLength = payload.Length + headerLength;

            var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
            try
            {
                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 0, 4), (uint)totalLength);
                BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(buffer, 4, 2), headerLength);
                BinaryPrimitives.WriteUInt16BigEndian(new Span<byte>(buffer, 6, 2), 1);
                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 8, 4), (uint)action);
                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 12, 4), 1);

                if (payload.Length > 0)
                    Array.Copy(payload, 0, buffer, headerLength, payload.Length);

                await transport.SendAsync(buffer, 0, totalLength).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        #endregion

        #region Receive

        private static async Task ProcessDataAsync(PipeReader reader, Action<string> callback)
        {
            while (true)
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;

                while (TryParseCommand(ref buffer, callback)) { }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    break;
            }
            await reader.CompleteAsync();
        }

        private static bool TryParseCommand(ref ReadOnlySequence<byte> buffer, Action<string> callback)
        {
            if (buffer.Length < 4)
                return false;

            int length;
            {
                var lengthSlice = buffer.Slice(buffer.Start, 4);
                if (lengthSlice.IsSingleSegment)
                {
                    length = BinaryPrimitives.ReadInt32BigEndian(lengthSlice.First.Span);
                }
                else
                {
                    Span<byte> stackBuffer = stackalloc byte[4];
                    lengthSlice.CopyTo(stackBuffer);
                    length = BinaryPrimitives.ReadInt32BigEndian(stackBuffer);
                }
            }

            if (buffer.Length < length)
                return false;

            var headerSlice = buffer.Slice(buffer.Start, 16);
            buffer = buffer.Slice(headerSlice.End);
            var bodySlice = buffer.Slice(buffer.Start, length - 16);
            buffer = buffer.Slice(bodySlice.End);

            DanmakuProtocol header;
            if (headerSlice.IsSingleSegment)
            {
                Parse2Protocol(headerSlice.First.Span, out header);
            }
            else
            {
                Span<byte> stackBuffer = stackalloc byte[16];
                headerSlice.CopyTo(stackBuffer);
                Parse2Protocol(stackBuffer, out header);
            }

            if (header.Version == 2 && header.Action == 5)
                ParseCommandDeflateBody(ref bodySlice, callback);
            else
                ParseCommandNormalBody(ref bodySlice, header.Action, callback);

            return true;
        }

        private static void ParseCommandDeflateBody(ref ReadOnlySequence<byte> buffer, Action<string> callback)
        {
            using var deflate = new DeflateStream(buffer.Slice(2, buffer.End).AsStream(), CompressionMode.Decompress, leaveOpen: false);
            var reader = PipeReader.Create(deflate);
            while (true)
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                // 全内存内运行同步返回，所以不会有问题
                var result = reader.ReadAsync().Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                var inner_buffer = result.Buffer;

                while (TryParseCommand(ref inner_buffer, callback)) { }

                reader.AdvanceTo(inner_buffer.Start, inner_buffer.End);

                if (result.IsCompleted)
                    break;
            }
            reader.Complete();
        }

        private static void ParseCommandNormalBody(ref ReadOnlySequence<byte> buffer, int action, Action<string> callback)
        {
            switch (action)
            {
                case 5:
                    {
                        if (buffer.Length > int.MaxValue)
                            throw new ArgumentOutOfRangeException("ParseCommandNormalBody buffer length larger than int.MaxValue");

                        var b = ArrayPool<byte>.Shared.Rent((int)buffer.Length);
                        try
                        {
                            buffer.CopyTo(b);
                            var json = Encoding.UTF8.GetString(b, 0, (int)buffer.Length);
                            callback(json);
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(b);
                        }
                    }
                    break;
                case 3:

                    break;
                default:
                    break;
            }
        }

        private static unsafe void Parse2Protocol(ReadOnlySpan<byte> buffer, out DanmakuProtocol protocol)
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
    }
}
