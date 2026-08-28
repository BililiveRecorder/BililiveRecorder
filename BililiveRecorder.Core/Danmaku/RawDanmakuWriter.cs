using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Danmaku;
using BililiveRecorder.Core.Config.V3;
using Serilog;

#nullable enable
namespace BililiveRecorder.Core.Danmaku
{
    /// <summary>
    /// Writes raw danmaku data received from the server to a JSONL file.
    /// Each line contains the complete raw JSON as received from the server.
    /// </summary>
    internal class RawDanmakuWriter : IRawDanmakuWriter
    {
        private StreamWriter? streamWriter = null;
        private uint writeCount = 0;
        private RoomConfig? config;

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly ILogger logger;

        public RawDanmakuWriter(ILogger logger)
        {
            this.logger = logger?.ForContext<RawDanmakuWriter>() ?? throw new ArgumentNullException(nameof(logger));
        }

        public void EnableWithPath(string path, IRoom room)
        {
            if (this.disposedValue) return;

            this.semaphoreSlim.Wait();
            try
            {
                if (this.streamWriter != null)
                {
                    this.streamWriter.Close();
                    this.streamWriter.Dispose();
                    this.streamWriter = null;
                }

                try { Directory.CreateDirectory(Path.GetDirectoryName(path)!); } catch (Exception) { }
                var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                this.config = room.RoomConfig;
                this.streamWriter = new StreamWriter(stream, new UTF8Encoding(false));
                this.writeCount = 0;
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        public void Disable()
        {
            if (this.disposedValue) return;

            this.semaphoreSlim.Wait();
            try
            {
                this.DisableCore();
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        private void DisableCore()
        {
            try
            {
                if (this.streamWriter != null)
                {
                    this.streamWriter.Close();
                    this.streamWriter.Dispose();
                    this.streamWriter = null;
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "关闭原始弹幕文件时发生错误");
                this.streamWriter = null;
            }
        }

        public async Task WriteAsync(DanmakuModel danmakuModel)
        {
            if (this.disposedValue)
                return;

            if (this.streamWriter is null || this.config is null)
                return;

            // Write all raw data without any filtering
            if (string.IsNullOrEmpty(danmakuModel.RawString))
                return;

            await this.semaphoreSlim.WaitAsync();
            try
            {
                if (this.streamWriter is null)
                    return;

                // Write the raw JSON string from the server directly (one JSON object per line)
                await this.streamWriter.WriteLineAsync(danmakuModel.RawString).ConfigureAwait(false);

                if (this.writeCount++ >= this.config.RecordDanmakuFlushInterval)
                {
                    await this.streamWriter.FlushAsync();
                    this.writeCount = 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "写入原始弹幕数据时发生错误");
                this.DisableCore();
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.semaphoreSlim.Dispose();
                    this.streamWriter?.Close();
                    this.streamWriter?.Dispose();
                    this.streamWriter = null;
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
