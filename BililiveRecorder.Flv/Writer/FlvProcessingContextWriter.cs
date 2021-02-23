using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using Serilog;

namespace BililiveRecorder.Flv.Writer
{
    public class FlvProcessingContextWriter : IFlvProcessingContextWriter, IDisposable
    {
        private static readonly byte[] FLV_FILE_HEADER = new byte[] { (byte)'F', (byte)'L', (byte)'V', 1, 0b0000_0101, 0, 0, 0, 9, 0, 0, 0, 0 };

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IFlvWriterTargetProvider targetProvider;
        private readonly IMemoryStreamProvider memoryStreamProvider;
        private readonly ILogger? logger;
        private bool disposedValue;
        private WriterState state = WriterState.EmptyFileOrNotOpen;

        private Stream? stream = null;
        private object? streamState = null;

        private Tag? nextScriptTag = null;
        private Tag? nextAudioHeaderTag = null;
        private Tag? nextVideoHeaderTag = null;

        private ScriptTagBody? lastScriptBody = null;
        private uint lastScriptBodyLength = 0;
        private double lastDuration;

        public event EventHandler<FileClosedEventArgs>? FileClosed;

        public Action<ScriptTagBody>? BeforeScriptTagWrite { get; set; }
        public Action<ScriptTagBody>? BeforeScriptTagRewrite { get; set; }

        public FlvProcessingContextWriter(IFlvWriterTargetProvider targetProvider, IMemoryStreamProvider memoryStreamProvider, ILogger? logger)
        {
            this.targetProvider = targetProvider ?? throw new ArgumentNullException(nameof(targetProvider));
            this.memoryStreamProvider = memoryStreamProvider ?? throw new ArgumentNullException(nameof(memoryStreamProvider));
            this.logger = logger?.ForContext<FlvProcessingContextWriter>();
        }

        public async Task WriteAsync(FlvProcessingContext context)
        {
            if (this.state == WriterState.Invalid)
                throw new InvalidOperationException("FlvProcessingContextWriter is in a invalid state.");

            // TODO disk speed detection
            //if (!await this.semaphoreSlim.WaitAsync(1000 * 5).ConfigureAwait(false))
            //{
            //    this.state = WriterState.Invalid;
            //    throw new InvalidOperationException("WriteAsync Wait timed out.");
            //}

            await this.semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                foreach (var item in context.Output)
                {
                    try
                    {
                        await this.WriteSingleActionAsync(item).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        this.state = WriterState.Invalid;
                        throw;
                    }
                }
            }
            finally
            {
                this.semaphoreSlim.Release();
            }

            // Dispose tags
            foreach (var action in context.Output)
                if (action is PipelineDataAction dataAction)
                    foreach (var tag in dataAction.Tags)
                        tag.BinaryData?.Dispose();
        }

        #region Flv Writer Implementation

        private Task WriteSingleActionAsync(PipelineAction action) => action switch
        {
            PipelineNewFileAction _ => this.OpenNewFile(),
            PipelineScriptAction scriptAction => this.WriteScriptTag(scriptAction),
            PipelineHeaderAction headerAction => this.WriteHeaderTags(headerAction),
            PipelineDataAction dataAction => this.WriteDataTags(dataAction),
            PipelineLogAlternativeHeaderAction logAlternativeHeaderAction => this.WriteAlternativeHeader(logAlternativeHeaderAction),
            _ => Task.CompletedTask,
        };

        private async Task WriteAlternativeHeader(PipelineLogAlternativeHeaderAction logAlternativeHeaderAction)
        {
            using var writer = new StreamWriter(this.targetProvider.CreateAlternativeHeaderStream(), Encoding.UTF8);
            await writer.WriteLineAsync("----- Group Start -----").ConfigureAwait(false);
            await writer.WriteLineAsync("连续遇到了多个不同的音视频Header，如果录制的文件不能正常播放可以尝试用这里的数据进行修复").ConfigureAwait(false);
            await writer.WriteLineAsync(DateTimeOffset.Now.ToString("O")).ConfigureAwait(false);

            foreach (var tag in logAlternativeHeaderAction.Tags)
            {
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.WriteLineAsync(tag.ToString()).ConfigureAwait(false);
                await writer.WriteLineAsync(tag.BinaryDataForSerializationUseOnly).ConfigureAwait(false);
            }

            await writer.WriteLineAsync("----- Group End -----").ConfigureAwait(false);
        }

        private Task OpenNewFile()
        {
            this.CloseCurrentFileImpl();
            // delay open until write
            this.state = WriterState.EmptyFileOrNotOpen;
            return Task.CompletedTask;
        }

        private Task WriteScriptTag(PipelineScriptAction scriptAction)
        {
            if (scriptAction.Tag != null)
                this.nextScriptTag = scriptAction.Tag;

            // delay writing
            return Task.CompletedTask;
        }

        private Task WriteHeaderTags(PipelineHeaderAction headerAction)
        {
            if (headerAction.AudioHeader != null)
                this.nextAudioHeaderTag = headerAction.AudioHeader;

            if (headerAction.VideoHeader != null)
                this.nextVideoHeaderTag = headerAction.VideoHeader;

            // delay writing
            return Task.CompletedTask;
        }

        private void CloseCurrentFileImpl()
        {
            if (this.stream is null)
                return;

            //await this.RewriteScriptTagImpl(0).ConfigureAwait(false);
            //await this.stream.FlushAsync().ConfigureAwait(false);

            var eventArgs = new FileClosedEventArgs
            {
                FileSize = this.stream.Length,
                Duration = this.lastDuration,
                State = this.streamState,
            };

            this.stream.Close();
            this.stream.Dispose();
            this.stream = null;

            this.streamState = null;
            this.lastDuration = 0d;

            FileClosed?.Invoke(this, eventArgs);
        }

        private async Task OpenNewFileImpl()
        {
            this.CloseCurrentFileImpl();

            Debug.Assert(this.stream is null, "stream is not null");

            (this.stream, this.streamState) = this.targetProvider.CreateOutputStream();
            await this.stream.WriteAsync(FLV_FILE_HEADER, 0, FLV_FILE_HEADER.Length).ConfigureAwait(false);

            this.state = WriterState.BeforeScript;
        }

        private async Task RewriteScriptTagImpl(double duration)
        {
            if (this.stream is null || this.lastScriptBody is null)
                return;

            var value = this.lastScriptBody.GetMetadataValue();
            if (!(value is null))
                value["duration"] = (ScriptDataNumber)duration;

            this.BeforeScriptTagRewrite?.Invoke(this.lastScriptBody);

            this.stream.Seek(9 + 4 + 11, SeekOrigin.Begin);

            using (var buf = this.memoryStreamProvider.CreateMemoryStream(nameof(FlvProcessingContextWriter) + ":" + nameof(RewriteScriptTagImpl) + ":Temp"))
            {
                this.lastScriptBody.WriteTo(buf);
                if (buf.Length == this.lastScriptBodyLength)
                {
                    buf.Seek(0, SeekOrigin.Begin);
                    await buf.CopyToAsync(this.stream);
                    await this.stream.FlushAsync();
                }
                else
                {
                    this.logger?.Warning("因 Script tag 输出长度不一致跳过修改");
                }
            }

            this.stream.Seek(0, SeekOrigin.End);
        }

        private async Task WriteScriptTagImpl()
        {
            if (this.nextScriptTag is null)
                throw new InvalidOperationException("No script tag availible");

            if (this.nextScriptTag.ScriptData is null)
                throw new InvalidOperationException("ScriptData is null");

            if (this.stream is null)
                throw new Exception("stream is null");

            this.lastScriptBody = this.nextScriptTag.ScriptData;

            var value = this.lastScriptBody.GetMetadataValue();
            if (!(value is null))
                value["duration"] = (ScriptDataNumber)0;

            this.BeforeScriptTagWrite?.Invoke(this.lastScriptBody);

            var bytes = ArrayPool<byte>.Shared.Rent(11);
            try
            {
                using var bodyStream = this.memoryStreamProvider.CreateMemoryStream(nameof(FlvProcessingContextWriter) + ":" + nameof(WriteScriptTagImpl) + ":Temp");
                this.lastScriptBody.WriteTo(bodyStream);
                this.lastScriptBodyLength = (uint)bodyStream.Length;
                bodyStream.Seek(0, SeekOrigin.Begin);

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 0, 4), this.lastScriptBodyLength);
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

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(bytes, 0, 4), this.lastScriptBodyLength + 11);
                await this.stream.WriteAsync(bytes, 0, 4).ConfigureAwait(false);

                await this.stream.FlushAsync();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
            this.state = WriterState.BeforeHeader;
        }

        private async Task WriteHeaderTagsImpl()
        {
            if (this.stream is null)
                throw new Exception("stream is null");

            if (this.nextVideoHeaderTag is null)
                throw new InvalidOperationException("No video header tag availible");

            if (this.nextAudioHeaderTag is null)
                throw new InvalidOperationException("No audio header tag availible");

            await this.nextVideoHeaderTag.WriteTo(this.stream, 0, this.memoryStreamProvider).ConfigureAwait(false);
            await this.nextAudioHeaderTag.WriteTo(this.stream, 0, this.memoryStreamProvider).ConfigureAwait(false);

            this.state = WriterState.Writing;
        }

        private async Task WriteDataTags(PipelineDataAction dataAction)
        {
            switch (this.state)
            {
                case WriterState.EmptyFileOrNotOpen:
                    await this.OpenNewFileImpl().ConfigureAwait(false);
                    await this.WriteScriptTagImpl().ConfigureAwait(false);
                    await this.WriteHeaderTagsImpl().ConfigureAwait(false);
                    break;
                case WriterState.BeforeScript:
                    await this.WriteScriptTagImpl().ConfigureAwait(false);
                    await this.WriteHeaderTagsImpl().ConfigureAwait(false);
                    break;
                case WriterState.BeforeHeader:
                    await this.WriteHeaderTagsImpl().ConfigureAwait(false);
                    break;
                case WriterState.Writing:
                    if (this.stream is null)
                        throw new Exception("stream is null");

                    if (this.targetProvider.ShouldCreateNewFile(this.stream, dataAction.Tags))
                    {
                        await this.OpenNewFileImpl().ConfigureAwait(false);
                        await this.WriteScriptTagImpl().ConfigureAwait(false);
                        await this.WriteHeaderTagsImpl().ConfigureAwait(false);
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Can't write data tag with current state ({this.state})");
            }

            if (this.stream is null)
                throw new Exception("stream is null");

            foreach (var tag in dataAction.Tags)
                await tag.WriteTo(this.stream, tag.Timestamp, this.memoryStreamProvider).ConfigureAwait(false);

            var duration = dataAction.Tags[dataAction.Tags.Count - 1].Timestamp / 1000d;
            this.lastDuration = duration;
            await this.RewriteScriptTagImpl(duration).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.CloseCurrentFileImpl();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FlvProcessingContextWriter()
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
    }

    internal enum WriterState
    {
        /// <summary>
        /// Invalid
        /// </summary>
        Invalid,
        /// <summary>
        /// 未开文件、空文件、还未写入 FLV Header
        /// </summary>
        EmptyFileOrNotOpen,
        /// <summary>
        /// 已写入 FLV Header、还未写入 Script Tag
        /// </summary>
        BeforeScript,
        /// <summary>
        /// 已写入 Script Tag、还未写入 音视频 Header
        /// </summary>
        BeforeHeader,
        /// <summary>
        /// 已写入音视频 Header、正常写入数据
        /// </summary>
        Writing,
    }
}
