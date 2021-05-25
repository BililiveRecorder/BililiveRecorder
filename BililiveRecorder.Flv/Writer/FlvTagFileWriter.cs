using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;
using Serilog;

namespace BililiveRecorder.Flv.Writer
{
    public class FlvTagFileWriter : IFlvTagWriter
    {
        private static readonly byte[] FLV_FILE_HEADER = new byte[] { (byte)'F', (byte)'L', (byte)'V', 1, 0b0000_0101, 0, 0, 0, 9, 0, 0, 0, 0 };

        private readonly IFlvWriterTargetProvider targetProvider;
        private readonly IMemoryStreamProvider memoryStreamProvider;
        private readonly ILogger? logger;

        private Stream? stream;
        private uint lastMetadataLength;
        private bool disposedValue;

        public FlvTagFileWriter(IFlvWriterTargetProvider targetProvider, IMemoryStreamProvider memoryStreamProvider, ILogger? logger)
        {
            this.targetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
            this.memoryStreamProvider = memoryStreamProvider ?? throw new ArgumentNullException(nameof(memoryStreamProvider));
            this.logger = logger?.ForContext<FlvTagFileWriter>();
        }

        public long FileSize => this.stream?.Length ?? 0;
        public object? State { get; private set; }

        public bool CloseCurrentFile()
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(FlvTagFileWriter));

            if (this.stream is null)
                return false;

            try
            { this.stream.Dispose(); }
            catch (Exception ex)
            { this.logger?.Warning(ex, "关闭文件时发生错误"); }

            this.stream = null;

            return true;
        }

        public async Task CreateNewFile()
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(FlvTagFileWriter));

            System.Diagnostics.Debug.Assert(this.stream is null, "stream is not null");
            this.stream?.Dispose();

            (this.stream, this.State) = this.targetProvider.CreateOutputStream();

            await this.stream.WriteAsync(FLV_FILE_HEADER, 0, FLV_FILE_HEADER.Length).ConfigureAwait(false);
        }

        public async Task OverwriteMetadata(ScriptTagBody metadata)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(FlvTagFileWriter));

            if (this.stream is null || metadata is null)
                return;

            using var buf = this.memoryStreamProvider.CreateMemoryStream(nameof(FlvTagFileWriter) + ":" + nameof(OverwriteMetadata) + ":Temp");
            metadata.WriteTo(buf);
            if (buf.Length == this.lastMetadataLength)
            {
                buf.Seek(0, SeekOrigin.Begin);
                try
                {
                    this.stream.Seek(9 + 4 + 11, SeekOrigin.Begin);
                    await buf.CopyToAsync(this.stream);
                    await this.stream.FlushAsync();
                }
                finally
                {
                    this.stream.Seek(0, SeekOrigin.End);
                }
            }
            else
            {
                this.logger?.Warning("因 Script tag 输出长度不一致跳过修改");
            }
        }

        public async Task WriteAlternativeHeaders(IEnumerable<Tag> tags)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(FlvTagFileWriter));

            using var writer = new StreamWriter(this.targetProvider.CreateAlternativeHeaderStream(), Encoding.UTF8);
            await writer.WriteLineAsync("----- Group Start -----").ConfigureAwait(false);
            await writer.WriteLineAsync("连续遇到了多个不同的音视频Header，如果录制的文件不能正常播放可以尝试用这里的数据进行修复").ConfigureAwait(false);
            await writer.WriteLineAsync(DateTimeOffset.Now.ToString("O")).ConfigureAwait(false);

            foreach (var tag in tags)
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync(tag.ToString()).ConfigureAwait(false);
                await writer.WriteLineAsync(tag.BinaryDataForSerializationUseOnly).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("----- Group End -----").ConfigureAwait(false);
        }

        public Task WriteTag(Tag tag)
        {
            if (this.disposedValue)
                throw new ObjectDisposedException(nameof(FlvTagFileWriter));

            if (this.stream is null)
                throw new InvalidOperationException("stream is null");

            return tag switch
            {
                Tag { Type: TagType.Script } => this.WriteScriptTagImpl(tag),
                Tag header when header.IsHeader() => header.WriteTo(this.stream, 0, this.memoryStreamProvider),
                Tag => tag.WriteTo(this.stream, tag.Timestamp, this.memoryStreamProvider),
                _ => Task.CompletedTask,
            };
        }

        private async Task WriteScriptTagImpl(Tag tag)
        {
            if (this.stream is null)
                throw new Exception("stream is null");

            if (tag.ScriptData is null)
                throw new Exception("Script Data is null");

            var bytes = ArrayPool<byte>.Shared.Rent(11);
            try
            {
                using var bodyStream = this.memoryStreamProvider.CreateMemoryStream(nameof(FlvTagFileWriter) + ":" + nameof(WriteScriptTagImpl) + ":Temp");
                tag.ScriptData.WriteTo(bodyStream);
                this.lastMetadataLength = (uint)bodyStream.Length;
                bodyStream.Seek(0, SeekOrigin.Begin);

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 0, 4), this.lastMetadataLength);
                bytes[0] = (byte)TagType.Script;

                bytes[4] = 0;
                bytes[5] = 0;
                bytes[6] = 0;
                bytes[7] = 0;
                bytes[8] = 0;
                bytes[9] = 0;
                bytes[10] = 0;

                await this.stream.WriteAsync(bytes, 0, 11).ConfigureAwait(false);
                await bodyStream.CopyToAsync(this.stream);

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 0, 4), this.lastMetadataLength + 11);
                await this.stream.WriteAsync(bytes, 0, 4).ConfigureAwait(false);

                await this.stream.FlushAsync();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        this.stream?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        this.logger?.Warning(ex, "关闭文件时发生错误");
                    }

                    this.stream = null;
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FlvTagFileWriter()
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
    }
}
