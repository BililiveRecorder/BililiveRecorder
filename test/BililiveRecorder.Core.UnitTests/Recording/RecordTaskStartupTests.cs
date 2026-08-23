using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Recording;
using BililiveRecorder.Core.Scripting;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Recording
{
    public class RecordTaskStartupTests
    {
        [Fact]
        public async Task IncompleteFlvStreamIsStoppedWhenNoFileIsOpenedAsync()
        {
            var server = new IncompleteFlvServer();
            var globalConfig = new GlobalConfig
            {
                FileNameRecordTemplate = "recording.flv",
                FlvWriteMetadata = false,
                NetworkTransportAllowedAddressFamily = AllowedAddressFamily.Ipv4,
                TimingStreamConnect = 5000,
                TimingWatchdogTimeout = 3000,
            };
            var roomConfig = new RoomConfig
            {
                RoomId = 25788785,
                RecordingQuality = "avc10000",
            };
            roomConfig.SetParent(globalConfig);

            var room = new TestRoom(roomConfig);
            var logger = new LoggerConfiguration().MinimumLevel.Verbose().CreateLogger();
            var apiClient = new TestApiClient(server.Uri);
            var userScriptRunner = new UserScriptRunner(globalConfig);

            var services = new ServiceCollection();
            services.AddSingleton<IMemoryStreamProvider, DefaultMemoryStreamProvider>();
            services.AddSingleton<ILogger>(logger);
            using var serviceProvider = services.BuildServiceProvider();

            var task = new StandardRecordTask(
                room,
                logger,
                new ProcessingPipelineBuilder(),
                apiClient,
                new FlvTagReaderFactory(serviceProvider),
                new TagGroupReaderFactory(),
                new FlvProcessingContextWriterWithFileWriterFactory(serviceProvider),
                userScriptRunner);

            var fileOpeningCount = 0;
            var networkStatsWithBytes = 0;
            var sessionEnded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            task.RecordFileOpening += (_, _) => Interlocked.Increment(ref fileOpeningCount);
            task.IOStats += (_, e) =>
            {
                if (e.NetworkBytesDownloaded > 0)
                    Interlocked.Increment(ref networkStatsWithBytes);
            };
            task.RecordSessionEnded += (_, _) => sessionEnded.TrySetResult(true);

            try
            {
                await task.StartAsync();

                Assert.True(task.IsReceiving);
                Assert.False(RoomLifecyclePolicy.ShouldCancelRecordTaskStartup(
                    streaming: false,
                    recordTaskExists: true,
                    recordTaskReceiving: task.IsReceiving));

                // The server keeps adding bytes, but never completes the first FLV tag.
                var completed = await Task.WhenAny(sessionEnded.Task, Task.Delay(TimeSpan.FromSeconds(7)));
                Assert.Same(sessionEnded.Task, completed);
                Assert.True(Volatile.Read(ref networkStatsWithBytes) > 0);
                Assert.Equal(0, Volatile.Read(ref fileOpeningCount));
            }
            finally
            {
                task.RequestStop();
                await server.DisposeAsync();
            }
        }

        private sealed class IncompleteFlvServer
        {
            private readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            private readonly Task servingTask;
            private volatile bool disposed;

            public IncompleteFlvServer()
            {
                this.listener.Start();
                var endpoint = (IPEndPoint)this.listener.LocalEndpoint;
                this.Uri = new Uri($"http://127.0.0.1:{endpoint.Port}/stream");
                this.servingTask = this.ServeAsync();
            }

            public Uri Uri { get; }

            private async Task ServeAsync()
            {
                try
                {
                    using var client = await this.listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    using var stream = client.GetStream();

                    var response = System.Text.Encoding.ASCII.GetBytes(
                        "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: video/x-flv\r\n" +
                        "Connection: keep-alive\r\n" +
                        "\r\n");
                    await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);

                    // FLV header + previous-tag-size + a tag header declaring a 1 MiB body.
                    var incompleteFlv = new byte[24];
                    incompleteFlv[0] = (byte)'F';
                    incompleteFlv[1] = (byte)'L';
                    incompleteFlv[2] = (byte)'V';
                    incompleteFlv[3] = 1;
                    incompleteFlv[4] = 5;
                    incompleteFlv[8] = 9;
                    incompleteFlv[13] = 18;
                    incompleteFlv[14] = 0x10;
                    incompleteFlv[15] = 0;
                    incompleteFlv[16] = 0;
                    await stream.WriteAsync(incompleteFlv, 0, incompleteFlv.Length).ConfigureAwait(false);

                    var oneByte = new byte[1];
                    while (!this.disposed)
                    {
                        await Task.Delay(250).ConfigureAwait(false);
                        await stream.WriteAsync(oneByte, 0, oneByte.Length).ConfigureAwait(false);
                    }
                }
                catch (Exception) when (this.disposed)
                {
                }
            }

            public async Task DisposeAsync()
            {
                if (this.disposed)
                    return;

                this.disposed = true;
                this.listener.Stop();
                try
                {
#pragma warning disable VSTHRD003
                    await this.servingTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003
                }
                catch (Exception)
                {
                }
            }
        }

        private sealed class TestApiClient : IApiClient
        {
            private readonly Uri streamUri;

            public TestApiClient(Uri streamUri)
            {
                this.streamUri = streamUri;
            }

            public Task<BilibiliApiResponse<RoomInfo>> GetRoomInfoAsync(int roomid) =>
                Task.FromResult(new BilibiliApiResponse<RoomInfo>());

            public Task<BilibiliApiResponse<RoomPlayInfo>> GetStreamUrlAsync(
                int roomid,
                int qn,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();

                return Task.FromResult(new BilibiliApiResponse<RoomPlayInfo>
                {
                    Code = 0,
                    Data = new RoomPlayInfo
                    {
                        PlayurlInfo = new RoomPlayInfo.PlayurlInfoClass
                        {
                            Playurl = new RoomPlayInfo.PlayurlClass
                            {
                                Streams = new[]
                                {
                                    new RoomPlayInfo.StreamItem
                                    {
                                        ProtocolName = "http_stream",
                                        Formats = new[]
                                        {
                                            new RoomPlayInfo.FormatItem
                                            {
                                                FormatName = "flv",
                                                Codecs = new[]
                                                {
                                                    new RoomPlayInfo.CodecItem
                                                    {
                                                        CodecName = "avc",
                                                        BaseUrl = this.streamUri.AbsolutePath,
                                                        CurrentQn = 10000,
                                                        AcceptQn = new[] { 10000 },
                                                        UrlInfos = new[]
                                                        {
                                                            new RoomPlayInfo.UrlInfoItem
                                                            {
                                                                Host = this.streamUri.GetLeftPart(UriPartial.Authority),
                                                            },
                                                        },
                                                    },
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                });
            }

            public void Dispose()
            {
            }
        }

        private sealed class TestRoom : IRoom
        {
            public TestRoom(RoomConfig roomConfig)
            {
                this.RoomConfig = roomConfig;
            }

            public Guid ObjectId { get; } = Guid.NewGuid();
            public RoomConfig RoomConfig { get; }
            public int ShortId => RoomConfig.RoomId;
            public string Name => "test";
            public long Uid => 1;
            public string Title => "test";
            public string AreaNameParent => "test";
            public string AreaNameChild => "test";
            public Newtonsoft.Json.Linq.JObject? RawBilibiliApiJsonData => null;
            public bool Recording => false;
            public bool Streaming => true;
            public bool DanmakuConnected => false;
            public bool AutoRecordForThisSession => true;
            public RoomStats Stats { get; } = new RoomStats();

            public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
            public event EventHandler<RecordSessionStartedEventArgs>? RecordSessionStarted { add { } remove { } }
            public event EventHandler<RecordSessionEndedEventArgs>? RecordSessionEnded { add { } remove { } }
            public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening { add { } remove { } }
            public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed { add { } remove { } }
            public event EventHandler<RecordingStatsEventArgs>? RecordingStats { add { } remove { } }
            public event EventHandler<IOStatsEventArgs>? IOStats { add { } remove { } }

            public void StartRecord() { }
            public void StopRecord() { }
            public void SplitOutput() { }
            public Task RefreshRoomInfoAsync() => Task.CompletedTask;
            public void MarkNextRecordShouldUseRawMode() { }
            public void Dispose() { }
        }
    }
}
