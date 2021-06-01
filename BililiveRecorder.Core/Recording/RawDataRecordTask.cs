using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Event;
using Serilog;

namespace BililiveRecorder.Core.Recording
{
    public class RawDataRecordTask : RecordTaskBase
    {
        private RecordFileOpeningEventArgs? fileOpeningEventArgs;

        public RawDataRecordTask(IRoom room,
                                 ILogger logger,
                                 IApiClient apiClient)
            : base(room: room,
                   logger: logger?.ForContext<RawDataRecordTask>().ForContext(LoggingContext.RoomId, room.RoomConfig.RoomId)!,
                   apiClient: apiClient)
        {
        }

        public override void SplitOutput() { }

        protected override void StartRecordingLoop(Stream stream)
        {
            var (fullPath, relativePath) = this.CreateFileName();

            try
            { Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); }
            catch (Exception) { }

            this.fileOpeningEventArgs = new RecordFileOpeningEventArgs(this.room)
            {
                SessionId = this.SessionId,
                FullPath = fullPath,
                RelativePath = relativePath,
                FileOpenTime = DateTimeOffset.Now,
            };
            this.OnRecordFileOpening(this.fileOpeningEventArgs);

            this.logger.Information("新建录制文件 {Path}", fullPath);

            var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);

            _ = Task.Run(async () => await this.WriteStreamToFileAsync(stream, file).ConfigureAwait(false));
        }

        private async Task WriteStreamToFileAsync(Stream stream, FileStream file)
        {
            try
            {
                var buffer = new byte[1024 * 8];
                this.timer.Start();

                while (!this.ct.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, this.ct).ConfigureAwait(false);
                    if (bytesRead == 0)
                        break;

                    Interlocked.Add(ref this.fillerDownloadedBytes, bytesRead);

                    await file.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
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
                this.timer.Stop();
                this.RequestStop();

                try
                {
                    var openingEventArgs = this.fileOpeningEventArgs;
                    if (openingEventArgs is not null)
                        this.OnRecordFileClosed(new RecordFileClosedEventArgs(this.room)
                        {
                            SessionId = this.SessionId,
                            FullPath = openingEventArgs.FullPath,
                            RelativePath = openingEventArgs.RelativePath,
                            FileOpenTime = openingEventArgs.FileOpenTime,
                            FileCloseTime = DateTimeOffset.Now,
                            Duration = 0,
                            FileSize = file.Length,
                        });
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Error calling OnRecordFileClosed");
                }

                try
                { file.Dispose(); }
                catch (Exception ex)
                { this.logger.Warning(ex, "关闭文件时发生错误"); }

                try
                { stream.Dispose(); }
                catch (Exception) { }

                this.OnRecordSessionEnded(EventArgs.Empty);

                this.logger.Information("录制结束");
            }
        }
    }
}
