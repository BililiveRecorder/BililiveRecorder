using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.ProcessingRules;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using Serilog;

namespace BililiveRecorder.Core.Recording
{
    public class StandardRecordTask : RecordTaskBase
    {
        private readonly IFlvTagReaderFactory flvTagReaderFactory;
        private readonly ITagGroupReaderFactory tagGroupReaderFactory;
        private readonly IFlvProcessingContextWriterFactory writerFactory;
        private readonly ProcessingDelegate pipeline;

        private readonly IFlvWriterTargetProvider targetProvider;
        private readonly StatsRule statsRule;
        private readonly SplitRule splitFileRule;

        private readonly FlvProcessingContext context = new FlvProcessingContext();
        private readonly IDictionary<object, object?> session = new Dictionary<object, object?>();

        private ITagGroupReader? reader;
        private IFlvProcessingContextWriter? writer;

        public StandardRecordTask(IRoom room,
                          ILogger logger,
                          IProcessingPipelineBuilder builder,
                          IApiClient apiClient,
                          IFlvTagReaderFactory flvTagReaderFactory,
                          ITagGroupReaderFactory tagGroupReaderFactory,
                          IFlvProcessingContextWriterFactory writerFactory)
            : base(room: room,
                   logger: logger?.ForContext<StandardRecordTask>().ForContext(LoggingContext.RoomId, room.RoomConfig.RoomId)!,
                   apiClient: apiClient)
        {
            this.flvTagReaderFactory = flvTagReaderFactory ?? throw new ArgumentNullException(nameof(flvTagReaderFactory));
            this.tagGroupReaderFactory = tagGroupReaderFactory ?? throw new ArgumentNullException(nameof(tagGroupReaderFactory));
            this.writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            this.statsRule = new StatsRule();
            this.splitFileRule = new SplitRule();

            this.statsRule.StatsUpdated += this.StatsRule_StatsUpdated;

            this.pipeline = builder
                .Add(this.statsRule)
                .Add(this.splitFileRule)
                .AddDefault()
                .AddRemoveFillerData()
                .Build();

            this.targetProvider = new WriterTargetProvider(this, paths =>
            {
                this.logger.ForContext(LoggingContext.RoomId, this.room.RoomConfig.RoomId).Information("新建录制文件 {Path}", paths.fullPath);

                var e = new RecordFileOpeningEventArgs(this.room)
                {
                    SessionId = this.SessionId,
                    FullPath = paths.fullPath,
                    RelativePath = paths.relativePath,
                    FileOpenTime = DateTimeOffset.Now,
                };
                this.OnRecordFileOpening(e);
                return e;
            });
        }

        public override void SplitOutput() => this.splitFileRule.SetSplitBeforeFlag();

        protected override void StartRecordingLoop(Stream stream)
        {
            var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            this.reader = this.tagGroupReaderFactory.CreateTagGroupReader(this.flvTagReaderFactory.CreateFlvTagReader(pipe.Reader));

            this.writer = this.writerFactory.CreateWriter(this.targetProvider);
            this.writer.BeforeScriptTagWrite = this.Writer_BeforeScriptTagWrite;
            this.writer.FileClosed += (sender, e) =>
            {
                var openingEventArgs = (RecordFileOpeningEventArgs)e.State!;
                this.OnRecordFileClosed(new RecordFileClosedEventArgs(this.room)
                {
                    SessionId = this.SessionId,
                    FullPath = openingEventArgs.FullPath,
                    RelativePath = openingEventArgs.RelativePath,
                    FileOpenTime = openingEventArgs.FileOpenTime,
                    FileCloseTime = DateTimeOffset.Now,
                    Duration = e.Duration,
                    FileSize = e.FileSize,
                });
            };

            _ = Task.Run(async () => await this.FillPipeAsync(stream, pipe.Writer).ConfigureAwait(false));

            _ = Task.Run(this.RecordingLoopAsync);
        }

        private async Task FillPipeAsync(Stream stream, PipeWriter writer)
        {
            const int minimumBufferSize = 1024;
            this.timer.Start();

            Exception? exception = null;
            try
            {
                while (!this.ct.IsCancellationRequested)
                {
                    var memory = writer.GetMemory(minimumBufferSize);
                    try
                    {
                        var bytesRead = await stream.ReadAsync(memory, this.ct).ConfigureAwait(false);
                        if (bytesRead == 0)
                            break;
                        writer.Advance(bytesRead);
                        Interlocked.Add(ref this.fillerDownloadedBytes, bytesRead);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        break;
                    }

                    var result = await writer.FlushAsync(this.ct).ConfigureAwait(false);
                    if (result.IsCompleted)
                        break;
                }
            }
            finally
            {
                this.timer.Stop();
                stream.Dispose();
                await writer.CompleteAsync(exception).ConfigureAwait(false);
            }
        }

        private async Task RecordingLoopAsync()
        {
            try
            {
                if (this.reader is null) return;
                if (this.writer is null) return;

                while (!this.ct.IsCancellationRequested)
                {
                    var group = await this.reader.ReadGroupAsync(this.ct).ConfigureAwait(false);

                    if (group is null)
                        break;

                    this.context.Reset(group, this.session);

                    this.pipeline(this.context);

                    if (this.context.Comments.Count > 0)
                        this.logger.Debug("修复逻辑输出 {@Comments}", this.context.Comments);

                    await this.writer.WriteAsync(this.context).ConfigureAwait(false);

                    if (this.context.Actions.Any(x => x is PipelineDisconnectAction))
                    {
                        this.logger.Information("根据修复逻辑的要求结束录制");
                        break;
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.Debug(ex, "录制被取消");
            }
            catch (IOException ex)
            {
                this.logger.Warning(ex, "录制时发生IO错误");
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "录制时发生了错误");
            }
            finally
            {
                this.reader?.Dispose();
                this.reader = null;
                this.writer?.Dispose();
                this.writer = null;
                this.RequestStop();

                this.OnRecordSessionEnded(EventArgs.Empty);

                this.logger.Information("录制结束");
            }
        }

        private void Writer_BeforeScriptTagWrite(ScriptTagBody scriptTagBody)
        {
            if (scriptTagBody.Values.Count == 2 && scriptTagBody.Values[1] is ScriptDataEcmaArray value)
            {
                var now = DateTimeOffset.Now;
                value["Title"] = (ScriptDataString)this.room.Title;
                value["Artist"] = (ScriptDataString)$"{this.room.Name} ({this.room.RoomConfig.RoomId})";
                value["Comment"] = (ScriptDataString)
                    ($"B站直播间 {this.room.RoomConfig.RoomId} 的直播录像\n" +
                    $"主播名: {this.room.Name}\n" +
                    $"直播标题: {this.room.Title}\n" +
                    $"直播分区: {this.room.AreaNameParent}·{this.room.AreaNameChild}\n" +
                    $"录制时间: {now:O}\n" +
                    $"服务器: {this.streamHost}\n" +
                    $"\n" +
                    $"使用 B站录播姬 录制 https://rec.danmuji.org\n" +
                    $"录播姬版本: {GitVersionInformation.FullSemVer}");
                value["BililiveRecorder"] = new ScriptDataEcmaArray
                {
                    ["RecordedBy"] = (ScriptDataString)"BililiveRecorder B站录播姬",
                    ["RecordedFrom"] = (ScriptDataString)(this.streamHost ?? string.Empty),
                    ["RecorderVersion"] = (ScriptDataString)GitVersionInformation.InformationalVersion,
                    ["StartTime"] = (ScriptDataDate)now,
                    ["RoomId"] = (ScriptDataString)this.room.RoomConfig.RoomId.ToString(),
                    ["ShortId"] = (ScriptDataString)this.room.ShortId.ToString(),
                    ["Name"] = (ScriptDataString)this.room.Name,
                    ["StreamTitle"] = (ScriptDataString)this.room.Title,
                    ["AreaNameParent"] = (ScriptDataString)this.room.AreaNameParent,
                    ["AreaNameChild"] = (ScriptDataString)this.room.AreaNameChild,
                };
            }
        }

        private void StatsRule_StatsUpdated(object sender, RecordingStatsEventArgs e)
        {
            switch (this.room.RoomConfig.CuttingMode)
            {
                case Config.V2.CuttingMode.ByTime:
                    if (e.FileMaxTimestamp > this.room.RoomConfig.CuttingNumber * (60u * 1000u))
                        this.splitFileRule.SetSplitBeforeFlag();
                    break;
                case Config.V2.CuttingMode.BySize:
                    if ((e.CurrnetFileSize + (e.OutputVideoByteCount * 1.1) + e.OutputAudioByteCount) / (1024d * 1024d) > this.room.RoomConfig.CuttingNumber)
                        this.splitFileRule.SetSplitBeforeFlag();
                    break;
            }

            this.OnRecordingStats(e);
        }

        internal class WriterTargetProvider : IFlvWriterTargetProvider
        {
            private readonly StandardRecordTask task;
            private readonly Func<(string fullPath, string relativePath), object> OnNewFile;

            private string last_path = string.Empty;

            public WriterTargetProvider(StandardRecordTask task, Func<(string fullPath, string relativePath), object> onNewFile)
            {
                this.task = task ?? throw new ArgumentNullException(nameof(task));
                this.OnNewFile = onNewFile ?? throw new ArgumentNullException(nameof(onNewFile));
            }

            public (Stream stream, object? state) CreateOutputStream()
            {
                var paths = this.task.CreateFileName();

                try
                { Directory.CreateDirectory(Path.GetDirectoryName(paths.fullPath)); }
                catch (Exception) { }

                this.last_path = paths.fullPath;
                var state = this.OnNewFile(paths);

                var stream = new FileStream(paths.fullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
                return (stream, state);
            }

            public Stream CreateAlternativeHeaderStream()
            {
                var path = string.IsNullOrWhiteSpace(this.last_path)
                    ? Path.ChangeExtension(this.task.CreateFileName().fullPath, "headers.txt")
                    : Path.ChangeExtension(this.last_path, "headers.txt");

                try
                { Directory.CreateDirectory(Path.GetDirectoryName(path)); }
                catch (Exception) { }

                var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
                return stream;
            }
        }
    }
}
