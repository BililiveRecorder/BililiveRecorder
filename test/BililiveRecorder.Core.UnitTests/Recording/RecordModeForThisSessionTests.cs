using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Api.Danmaku;
using BililiveRecorder.Core.Api.Model;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Danmaku;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.Recording;
using BililiveRecorder.Core.Scripting;
using BililiveRecorder.Flv.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Recording
{
    public class RecordModeForThisSessionTests
    {
        private const int InitDelayFactor_DontStartBackgroundTasks = 4_000_000;

        [Theory]
        [InlineData(RecordMode.Standard)]
        [InlineData(RecordMode.RawData)]
        public void NotRecording_Returns_ConfigRecordMode(RecordMode configRecordMode)
        {
            using var room = CreateRoom(configRecordMode);

            Assert.False(room.Recording);
            Assert.Equal(configRecordMode, room.RecordModeForThisSession);
        }

        [Fact]
        public void NotRecording_ConfigRecordMode_Change_Raises_PropertyChanged()
        {
            using var room = CreateRoom(RecordMode.Standard);

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            room.RoomConfig.RecordMode = RecordMode.RawData;

            Assert.Equal(RecordMode.RawData, room.RecordModeForThisSession);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);
        }

        [Fact]
        public void Recording_StandardRecordTask_Returns_Standard()
        {
            using var room = CreateRoom(RecordMode.RawData);

            InvokeSetRecordTask(room, CreateUninitializedRecordTask<StandardRecordTask>());

            Assert.True(room.Recording);
            Assert.Equal(RecordMode.Standard, room.RecordModeForThisSession);

            InvokeSetRecordTask(room, null);
        }

        [Fact]
        public void Recording_RawDataRecordTask_Returns_RawData()
        {
            using var room = CreateRoom(RecordMode.Standard);

            InvokeSetRecordTask(room, CreateUninitializedRecordTask<RawDataRecordTask>());

            Assert.True(room.Recording);
            Assert.Equal(RecordMode.RawData, room.RecordModeForThisSession);

            InvokeSetRecordTask(room, null);
        }

        [Fact]
        public void Recording_UnknownRecordTask_FallsBack_To_ConfigRecordMode()
        {
            using var room = CreateRoom(RecordMode.RawData);

            InvokeSetRecordTask(room, new DummyRecordTask());

            Assert.True(room.Recording);
            Assert.Equal(RecordMode.RawData, room.RecordModeForThisSession);
        }

        [Fact]
        public void RecordModeForThisSession_Raises_PropertyChanged_On_Start_End_And_Switch()
        {
            using var room = CreateRoom(RecordMode.Standard);

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            InvokeSetRecordTask(room, CreateUninitializedRecordTask<StandardRecordTask>());

            Assert.Equal(RecordMode.Standard, room.RecordModeForThisSession);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);

            notified.Clear();
            InvokeSetRecordTask(room, null);

            Assert.False(room.Recording);
            Assert.Equal(RecordMode.Standard, room.RecordModeForThisSession);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);

            notified.Clear();
            InvokeSetRecordTask(room, CreateUninitializedRecordTask<RawDataRecordTask>());

            Assert.Equal(RecordMode.RawData, room.RecordModeForThisSession);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);

            InvokeSetRecordTask(room, null);
        }

        [Fact]
        public void RecordTask_RecordSessionEnded_Raises_PropertyChanged()
        {
            using var room = CreateRoom(RecordMode.Standard);

            InvokeSetRecordTask(room, new DummyRecordTask());

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            notified.Clear();
            InvokePrivateMethod(room, "RecordTask_RecordSessionEnded", null, EventArgs.Empty);

            Assert.False(room.Recording);
            Assert.Equal(RecordMode.Standard, room.RecordModeForThisSession);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);
        }

        [Fact]
        public async Task CreateAndStartNewRecordTask_WhenStartAsyncThrowsNoMatchingQnValue_ClearsTaskAndNotifiesAsync()
        {
            var recordTaskFactory = new SingleRecordTaskFactory(CreateUninitializedRecordTask<NoMatchingQnValueStandardRecordTask>());
            using var room = CreateRoom(RecordMode.Standard, recordTaskFactory);

            SetPrivateField(room, "streaming", true);
            SetPrivateField(room, "autoRecordForThisSession", false);

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            InvokePrivateMethod(room, "CreateAndStartNewRecordTask", true);

            await WaitUntilAsync(() => !room.Recording, TimeSpan.FromSeconds(1));

            Assert.Contains(nameof(room.RecordModeForThisSession), notified);
            Assert.False(room.Recording);
        }

        [Fact]
        public async Task CreateAndStartNewRecordTask_WhenStartAsyncThrowsException_ClearsTaskAndNotifiesAsync()
        {
            var recordTaskFactory = new SingleRecordTaskFactory(CreateUninitializedRecordTask<GenericExceptionStandardRecordTask>());
            using var room = CreateRoom(RecordMode.Standard, recordTaskFactory);

            SetPrivateField(room, "streaming", true);
            SetPrivateField(room, "autoRecordForThisSession", false);

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            InvokePrivateMethod(room, "CreateAndStartNewRecordTask", true);

            await WaitUntilAsync(() => !room.Recording, TimeSpan.FromSeconds(1));

            Assert.Contains(nameof(room.RecordModeForThisSession), notified);
            Assert.False(room.Recording);
        }

        [Fact]
        public async Task Fallback_Switches_From_Standard_To_RawData_And_NotifiesAsync()
        {
            var recordTaskFactory = new FallbackRecordTaskFactory();
            using var room = CreateRoom(RecordMode.Standard, recordTaskFactory);

            SetPrivateField(room, "streaming", true);

            var notified = new List<string?>();
            room.PropertyChanged += (_, e) => notified.Add(e.PropertyName);

            InvokePrivateMethod(room, "CreateAndStartNewRecordTask", true);

            Assert.Equal(RecordMode.Standard, room.RecordModeForThisSession);
            Assert.NotNull(recordTaskFactory.StandardTask);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);

            notified.Clear();
            room.MarkNextRecordShouldUseRawMode();
            recordTaskFactory.StandardTask!.TriggerSessionEnded();

            await WaitUntilAsync(() => room.RecordModeForThisSession == RecordMode.RawData, TimeSpan.FromSeconds(1));

            Assert.NotNull(recordTaskFactory.RawDataTask);
            Assert.Equal(new RecordMode?[] { null, RecordMode.RawData }, recordTaskFactory.RecordModeOverrides);
            Assert.Contains(nameof(room.RecordModeForThisSession), notified);
        }

        private static Room CreateRoom(RecordMode recordMode, IRecordTaskFactory? recordTaskFactory = null)
        {
            var globalConfig = new GlobalConfig
            {
                TimingCheckInterval = 180u,
            };

            var roomConfig = new RoomConfig
            {
                RoomId = 123,
                RecordMode = recordMode,
            };
            roomConfig.SetParent(globalConfig);

            var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .CreateLogger();

            return new Room(
                scope: new DummyServiceScope(),
                roomConfig: roomConfig,
                initDelayFactor: InitDelayFactor_DontStartBackgroundTasks,
                logger: logger,
                danmakuClient: new DummyDanmakuClient(),
                apiClient: new DummyApiClient(),
                basicDanmakuWriter: new DummyBasicDanmakuWriter(),
                recordTaskFactory: recordTaskFactory ?? new DummyRecordTaskFactory(),
                userScriptRunner: new UserScriptRunner(globalConfig));
        }

        private static TRecordTask CreateUninitializedRecordTask<TRecordTask>() where TRecordTask : IRecordTask
        {
#if NET6_0_OR_GREATER
            return (TRecordTask)RuntimeHelpers.GetUninitializedObject(typeof(TRecordTask));
#else
            return (TRecordTask)FormatterServices.GetUninitializedObject(typeof(TRecordTask));
#endif
        }

        private static void InvokeSetRecordTask(Room room, IRecordTask? recordTask) =>
            InvokePrivateMethod(room, "SetRecordTask", recordTask);

        private static void SetPrivateField<T>(Room room, string fieldName, T value)
        {
            var field = typeof(Room).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field!.SetValue(room, value);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
        {
            var start = DateTimeOffset.UtcNow;
            while (!condition())
            {
                if (DateTimeOffset.UtcNow - start > timeout)
                    throw new TimeoutException($"Condition not met within {timeout}.");
                await Task.Delay(10);
            }
        }

        private static void InvokePrivateMethod(Room room, string methodName, params object?[] args)
        {
            var method = typeof(Room).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method!.Invoke(room, args);
        }

        private sealed class DummyServiceScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new DummyServiceProvider();
            public void Dispose() { }

            private sealed class DummyServiceProvider : IServiceProvider
            {
                public object? GetService(Type serviceType) => null;
            }
        }

        private sealed class DummyDanmakuClient : IDanmakuClient
        {
            public bool Connected { get; private set; }

            public event EventHandler<StatusChangedEventArgs>? StatusChanged;
            public event EventHandler<DanmakuReceivedEventArgs>? DanmakuReceived;

            public Func<string, string?>? BeforeHandshake { get; set; }

            public Task ConnectAsync(int roomid, DanmakuTransportMode transportMode, CancellationToken cancellationToken)
            {
                this.Connected = true;
                this.StatusChanged?.Invoke(this, StatusChangedEventArgs.True);
                return Task.CompletedTask;
            }

            public Task DisconnectAsync()
            {
                this.Connected = false;
                this.StatusChanged?.Invoke(this, StatusChangedEventArgs.False);
                return Task.CompletedTask;
            }

            public void Dispose() { }

            public void RaiseDanmakuReceived(DanmakuReceivedEventArgs e) => this.DanmakuReceived?.Invoke(this, e);
        }

        private sealed class DummyApiClient : IApiClient
        {
            public Task<BilibiliApiResponse<RoomInfo>> GetRoomInfoAsync(int roomid) =>
                Task.FromResult(new BilibiliApiResponse<RoomInfo> { Data = null });

            public Task<BilibiliApiResponse<RoomPlayInfo>> GetStreamUrlAsync(int roomid, int qn) =>
                Task.FromResult(new BilibiliApiResponse<RoomPlayInfo> { Data = null });

            public void Dispose() { }
        }

        private sealed class DummyBasicDanmakuWriter : IBasicDanmakuWriter
        {
            public void Disable() { }
            public void EnableWithPath(string path, IRoom room) { }
            public Task WriteAsync(DanmakuModel danmakuModel) => Task.CompletedTask;
            public void Dispose() { }
        }

        private sealed class DummyRecordTaskFactory : IRecordTaskFactory
        {
            public IRecordTask CreateRecordTask(IRoom room, RecordMode? recordModeOverride = null) => new DummyRecordTask();
        }

#pragma warning disable CS0067 // The event is never used
        private sealed class DummyRecordTask : IRecordTask
        {
            public Guid SessionId { get; } = Guid.NewGuid();

            public event EventHandler<IOStatsEventArgs>? IOStats;
            public event EventHandler<RecordingStatsEventArgs>? RecordingStats;
            public event EventHandler<RecordFileOpeningEventArgs>? RecordFileOpening;
            public event EventHandler<RecordFileClosedEventArgs>? RecordFileClosed;
            public event EventHandler? RecordSessionEnded;

            public void SplitOutput() { }
            public Task StartAsync() => Task.CompletedTask;
            public void RequestStop() => this.RecordSessionEnded?.Invoke(this, EventArgs.Empty);
        }
#pragma warning restore CS0067 // The event is never used

        private sealed class SingleRecordTaskFactory : IRecordTaskFactory
        {
            private readonly IRecordTask task;
            public SingleRecordTaskFactory(IRecordTask task) => this.task = task;

            public IRecordTask CreateRecordTask(IRoom room, RecordMode? recordModeOverride = null) => this.task;
        }

        private sealed class FallbackRecordTaskFactory : IRecordTaskFactory
        {
            public TestStandardRecordTask? StandardTask { get; private set; }
            public TestRawDataRecordTask? RawDataTask { get; private set; }
            public List<RecordMode?> RecordModeOverrides { get; } = new();

            public IRecordTask CreateRecordTask(IRoom room, RecordMode? recordModeOverride = null)
            {
                this.RecordModeOverrides.Add(recordModeOverride);

                if (recordModeOverride == RecordMode.RawData)
                {
                    this.RawDataTask ??= CreateUninitializedRecordTask<TestRawDataRecordTask>();
                    return this.RawDataTask;
                }

                this.StandardTask ??= CreateUninitializedRecordTask<TestStandardRecordTask>();
                return this.StandardTask;
            }
        }

        private sealed class TestStandardRecordTask : StandardRecordTask
        {
            private TestStandardRecordTask()
                : base(room: null!,
                       logger: null!,
                       builder: null!,
                       apiClient: null!,
                       flvTagReaderFactory: null!,
                       tagGroupReaderFactory: null!,
                       writerFactory: null!,
                       userScriptRunner: null!)
            { }

            public override Task StartAsync() => Task.CompletedTask;
            public override void RequestStop() { }

            public void TriggerSessionEnded() => this.OnRecordSessionEnded(EventArgs.Empty);
        }

        private sealed class TestRawDataRecordTask : RawDataRecordTask
        {
            private TestRawDataRecordTask()
                : base(room: null!, logger: null!, apiClient: null!, userScriptRunner: null!)
            { }

            public override Task StartAsync() => Task.CompletedTask;
            public override void RequestStop() { }
        }

        private sealed class NoMatchingQnValueStandardRecordTask : StandardRecordTask
        {
            private NoMatchingQnValueStandardRecordTask()
                : base(room: null!,
                       logger: null!,
                       builder: null!,
                       apiClient: null!,
                       flvTagReaderFactory: null!,
                       tagGroupReaderFactory: null!,
                       writerFactory: null!,
                       userScriptRunner: null!)
            { }

            public override Task StartAsync() => Task.FromException(new NoMatchingQnValueException());
            public override void RequestStop() { }
        }

        private sealed class GenericExceptionStandardRecordTask : StandardRecordTask
        {
            private GenericExceptionStandardRecordTask()
                : base(room: null!,
                       logger: null!,
                       builder: null!,
                       apiClient: null!,
                       flvTagReaderFactory: null!,
                       tagGroupReaderFactory: null!,
                       writerFactory: null!,
                       userScriptRunner: null!)
            { }

            public override Task StartAsync() => Task.FromException(new InvalidOperationException("Test exception"));
            public override void RequestStop() { }
        }
    }
}
