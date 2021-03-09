using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping.Rules;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv.Grouping
{
    public class TagGroupReader : ITagGroupReader
    {
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly bool leaveOpen;
        private bool disposedValue;

        private List<Tag>? leftover;

        public IFlvTagReader TagReader { get; }
        public IList<IGroupingRule> GroupingRules { get; }

        public TagGroupReader(IFlvTagReader tagReader)
           : this(tagReader, false)
        { }

        public TagGroupReader(IFlvTagReader flvTagReader, bool leaveOpen = false)
        {
            this.TagReader = flvTagReader ?? throw new ArgumentNullException(nameof(flvTagReader));
            this.leaveOpen = leaveOpen;

            this.GroupingRules = new List<IGroupingRule>
            {
                new ScriptGroupingRule(),
                new EndGroupingRule(),
                new HeaderGroupingRule(),
                new DataGroupingRule()
            };
        }

        public async Task<PipelineAction?> ReadGroupAsync(CancellationToken token)
        {
            if (!this.semaphoreSlim.Wait(0))
            {
                throw new InvalidOperationException("Concurrent read is not supported.");
            }
            try
            {
                List<Tag> tags;

                if (this.leftover is not null)
                {
                    tags = this.leftover;
                    this.leftover = null;
                }
                else
                {
                    var firstTag = await this.TagReader.ReadTagAsync(token).ConfigureAwait(false);

                    // 数据已经全部读完
                    if (firstTag is null)
                        return null;

                    tags = new List<Tag> { firstTag };
                }

                var rule = this.GroupingRules.FirstOrDefault(x => x.StartWith(tags));

                if (rule is null)
                    throw new Exception("No grouping rule accepting tags:\n" + string.Join("\n", tags.Select(x => x.ToString())));

                while (!token.IsCancellationRequested)
                {
                    var tag = await this.TagReader.ReadTagAsync(token).ConfigureAwait(false);

                    if (tag == null || !rule.AppendWith(tag, tags, out this.leftover))
                    {
                        break;
                    }
                }

                return rule.CreatePipelineAction(tags);
            }
            finally
            {
                this.semaphoreSlim.Release();
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
                    if (!this.leaveOpen)
                        this.TagReader.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TagGroupReader()
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
}
