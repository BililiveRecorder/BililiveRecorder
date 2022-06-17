using System;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using Serilog;

namespace BililiveRecorder.Flv.Writer
{
    public class FlvProcessingContextWriter : IFlvProcessingContextWriter, IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IFlvTagWriter tagWriter;
        private readonly bool allowMissingHeader;
        private readonly bool disableKeyframes;
        private readonly ILogger? logger;
        private bool disposedValue;

        private WriterState state = WriterState.EmptyFileOrNotOpen;

        private Tag? nextScriptTag = null;
        private Tag? nextAudioHeaderTag = null;
        private Tag? nextVideoHeaderTag = null;

        private ScriptTagBody? lastScriptBody = null;
        private KeyframesScriptDataValue? keyframesScriptDataValue = null;
        private double lastDuration;

        private int bytesWrittenByCurrentWriteCall { get; set; }

        public event EventHandler<FileClosedEventArgs>? FileClosed;

        public Action<ScriptTagBody>? BeforeScriptTagWrite { get; set; }
        public Action<ScriptTagBody>? BeforeScriptTagRewrite { get; set; }

        public FlvProcessingContextWriter(IFlvTagWriter tagWriter, bool allowMissingHeader, bool disableKeyframes, ILogger? logger)
        {
            this.tagWriter = tagWriter ?? throw new ArgumentNullException(nameof(tagWriter));
            this.allowMissingHeader = allowMissingHeader;
            this.disableKeyframes = disableKeyframes;
            this.logger = logger?.ForContext<FlvProcessingContextWriter>();
        }

        public async Task<int> WriteAsync(FlvProcessingContext context)
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
                foreach (var item in context.Actions)
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

            var bytesWritten = this.bytesWrittenByCurrentWriteCall;
            this.bytesWrittenByCurrentWriteCall = 0;

            // Dispose tags
            foreach (var action in context.Actions)
                if (action is PipelineDataAction dataAction)
                    foreach (var tag in dataAction.Tags)
                        tag.BinaryData?.Dispose();

            return bytesWritten;
        }

        #region Flv Writer Implementation

        private Task WriteSingleActionAsync(PipelineAction action) => action switch
        {
            PipelineNewFileAction _ => this.OpenNewFile(),
            PipelineScriptAction scriptAction => this.WriteScriptTag(scriptAction),
            PipelineHeaderAction headerAction => this.WriteHeaderTags(headerAction),
            PipelineDataAction dataAction => this.WriteDataTags(dataAction),
            PipelineEndAction endAction => this.WriteEndTag(endAction),
            PipelineLogMessageWithLocationAction pipelineLogMessageWithLocationAction => this.LogMessageWithLocation(pipelineLogMessageWithLocationAction),
            _ => Task.CompletedTask,
        };

        private async Task LogMessageWithLocation(PipelineLogMessageWithLocationAction logMessageWithLocationAction)
        {
            this.logger?.Debug("写入录制记录，位置：视频时间 {FileDuration} 秒, 文件位置 {FileSize} 字节。\n{Message}", this.lastDuration, this.tagWriter.FileSize, logMessageWithLocationAction.Message);
            await this.tagWriter.WriteAccompanyingTextLog(this.lastDuration, logMessageWithLocationAction.Message).ConfigureAwait(false);
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
            var eventArgs = new FileClosedEventArgs
            {
                FileSize = this.tagWriter.FileSize,
                Duration = this.lastDuration,
                State = this.tagWriter.State,
            };

            if (this.tagWriter.CloseCurrentFile())
            {
                this.lastDuration = 0d;
                FileClosed?.Invoke(this, eventArgs);
            }
        }

        private async Task RewriteScriptTagImpl(double duration, bool updateKeyframes, double keyframeTime, double filePosition)
        {
            if (this.lastScriptBody is null)
                return;

            var value = this.lastScriptBody.GetMetadataValue();
            if (value is not null)
                value["duration"] = (ScriptDataNumber)duration;

            if (updateKeyframes && this.keyframesScriptDataValue is not null)
                this.keyframesScriptDataValue.AddData(time_in_ms: keyframeTime, filePosition: filePosition);

            this.BeforeScriptTagRewrite?.Invoke(this.lastScriptBody);

            await this.tagWriter.OverwriteMetadata(this.lastScriptBody).ConfigureAwait(false);
        }

        private async Task OpenNewFileThenWriteHeadersImpl()
        {
            this.CloseCurrentFileImpl();

            if (this.nextScriptTag is null || this.nextScriptTag.ScriptData is null)
            {
                // SRT + d1--ov-gotcha05.bilivideo.com 会导致无 script tag
                // 直接 new 一个出来用于存放录播姬写入的 metadata
                this.nextScriptTag = new Tag
                {
                    Type = TagType.Script,
                    Timestamp = 0,
                    ScriptData = new ScriptTagBody(new() { (ScriptDataString)"onMetaData", new ScriptDataEcmaArray() })
                };
            }

            if (!this.allowMissingHeader)
            {
                if (this.nextVideoHeaderTag is null)
                    throw new InvalidOperationException("No video header tag availible");

                if (this.nextAudioHeaderTag is null)
                    throw new InvalidOperationException("No audio header tag availible");
            }

            // Open File
            await this.tagWriter.CreateNewFile().ConfigureAwait(false);

            // Write Script Tag
            {
                this.lastScriptBody = this.nextScriptTag.ScriptData;

                var value = this.lastScriptBody.GetMetadataValue();
                if (value is not null)
                {
                    value["duration"] = (ScriptDataNumber)0;

                    if (!this.disableKeyframes)
                    {
                        var kfv = new KeyframesScriptDataValue();
                        value["keyframes"] = kfv;
                        this.keyframesScriptDataValue = kfv;
                    }
                }

                this.BeforeScriptTagWrite?.Invoke(this.lastScriptBody);

                await this.tagWriter.WriteTag(this.nextScriptTag).ConfigureAwait(false);
            }

            // Write Header Tag
            {
                if (this.nextVideoHeaderTag is not null)
                    await this.tagWriter.WriteTag(this.nextVideoHeaderTag).ConfigureAwait(false);

                if (this.nextAudioHeaderTag is not null)
                    await this.tagWriter.WriteTag(this.nextAudioHeaderTag).ConfigureAwait(false);
            }

            this.bytesWrittenByCurrentWriteCall += (int)this.tagWriter.FileSize;

            this.state = WriterState.Writing;
        }

        private async Task WriteDataTags(PipelineDataAction dataAction)
        {
            switch (this.state)
            {
                case WriterState.EmptyFileOrNotOpen:
                    await this.OpenNewFileThenWriteHeadersImpl().ConfigureAwait(false);
                    break;
                case WriterState.Writing:
                    break;
                default:
                    throw new InvalidOperationException($"Can't write data tag with current state ({this.state})");
            }

            var pos = this.tagWriter.FileSize;
            var tags = dataAction.Tags;
            var firstTag = tags[0];
            var duration = tags[tags.Count - 1].Timestamp / 1000d;
            this.lastDuration = duration;

            var beforeFileSize = this.tagWriter.FileSize;

            foreach (var tag in tags)
                await this.tagWriter.WriteTag(tag).ConfigureAwait(false);

            this.bytesWrittenByCurrentWriteCall += (int)(this.tagWriter.FileSize - beforeFileSize);

            await this.RewriteScriptTagImpl(duration, firstTag.IsKeyframeData(), firstTag.Timestamp, pos).ConfigureAwait(false);
        }

        private async Task WriteEndTag(PipelineEndAction endAction)
        {
            switch (this.state)
            {
                case WriterState.EmptyFileOrNotOpen:
                    await this.OpenNewFileThenWriteHeadersImpl().ConfigureAwait(false);
                    break;
                case WriterState.Writing:
                    break;
                default:
                    throw new InvalidOperationException($"Can't write data tag with current state ({this.state})");
            }

            var beforeFileSize = this.tagWriter.FileSize;

            await this.tagWriter.WriteTag(endAction.Tag).ConfigureAwait(false);

            this.bytesWrittenByCurrentWriteCall += (int)(this.tagWriter.FileSize - beforeFileSize);
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
                    try
                    {
                        this.CloseCurrentFileImpl();
                    }
                    catch (Exception)
                    { }

                    this.tagWriter.Dispose();
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
