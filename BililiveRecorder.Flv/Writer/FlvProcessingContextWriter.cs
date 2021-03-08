using System;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Writer
{
    public class FlvProcessingContextWriter : IFlvProcessingContextWriter, IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly IFlvTagWriter tagWriter;
        private bool disposedValue;

        private WriterState state = WriterState.EmptyFileOrNotOpen;

        private Tag? nextScriptTag = null;
        private Tag? nextAudioHeaderTag = null;
        private Tag? nextVideoHeaderTag = null;

        private ScriptTagBody? lastScriptBody = null;
        private double lastDuration;

        public event EventHandler<FileClosedEventArgs>? FileClosed;

        public Action<ScriptTagBody>? BeforeScriptTagWrite { get; set; }
        public Action<ScriptTagBody>? BeforeScriptTagRewrite { get; set; }

        public FlvProcessingContextWriter(IFlvTagWriter tagWriter)
        {
            this.tagWriter = tagWriter ?? throw new ArgumentNullException(nameof(tagWriter));
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

            // Dispose tags
            foreach (var action in context.Actions)
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
            PipelineEndAction endAction => this.WriteEndTag(endAction),
            PipelineLogAlternativeHeaderAction logAlternativeHeaderAction => this.WriteAlternativeHeader(logAlternativeHeaderAction),
            _ => Task.CompletedTask,
        };

        private Task WriteAlternativeHeader(PipelineLogAlternativeHeaderAction logAlternativeHeaderAction) =>
            this.tagWriter.WriteAlternativeHeaders(logAlternativeHeaderAction.Tags);

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

        private async Task OpenNewFileImpl()
        {
            this.CloseCurrentFileImpl();

            await this.tagWriter.CreateNewFile().ConfigureAwait(false);

            this.state = WriterState.BeforeScript;
        }

        private async Task RewriteScriptTagImpl(double duration)
        {
            if (this.lastScriptBody is null)
                return;

            var value = this.lastScriptBody.GetMetadataValue();
            if (value is not null)
                value["duration"] = (ScriptDataNumber)duration;

            this.BeforeScriptTagRewrite?.Invoke(this.lastScriptBody);

            await this.tagWriter.OverwriteMetadata(this.lastScriptBody).ConfigureAwait(false);
        }

        private async Task WriteScriptTagImpl()
        {
            if (this.nextScriptTag is null)
                throw new InvalidOperationException("No script tag availible");

            if (this.nextScriptTag.ScriptData is null)
                throw new InvalidOperationException("ScriptData is null");

            this.lastScriptBody = this.nextScriptTag.ScriptData;

            var value = this.lastScriptBody.GetMetadataValue();
            if (value is not null)
                value["duration"] = (ScriptDataNumber)0;

            this.BeforeScriptTagWrite?.Invoke(this.lastScriptBody);

            await this.tagWriter.WriteTag(this.nextScriptTag).ConfigureAwait(false);

            this.state = WriterState.BeforeHeader;
        }

        private async Task WriteHeaderTagsImpl()
        {
            if (this.nextVideoHeaderTag is null)
                throw new InvalidOperationException("No video header tag availible");

            if (this.nextAudioHeaderTag is null)
                throw new InvalidOperationException("No audio header tag availible");

            await this.tagWriter.WriteTag(this.nextVideoHeaderTag).ConfigureAwait(false);
            await this.tagWriter.WriteTag(this.nextAudioHeaderTag).ConfigureAwait(false);

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
                    break;
                default:
                    throw new InvalidOperationException($"Can't write data tag with current state ({this.state})");
            }

            foreach (var tag in dataAction.Tags)
                await this.tagWriter.WriteTag(tag).ConfigureAwait(false);

            var duration = dataAction.Tags[dataAction.Tags.Count - 1].Timestamp / 1000d;
            this.lastDuration = duration;
            await this.RewriteScriptTagImpl(duration).ConfigureAwait(false);
        }

        private async Task WriteEndTag(PipelineEndAction endAction)
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
                    break;
                default:
                    throw new InvalidOperationException($"Can't write data tag with current state ({this.state})");
            }

            await this.tagWriter.WriteTag(endAction.Tag).ConfigureAwait(false);
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
