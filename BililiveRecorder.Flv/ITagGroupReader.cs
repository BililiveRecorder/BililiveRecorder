using System;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;

namespace BililiveRecorder.Flv
{
    public interface ITagGroupReader : IDisposable
    {
        Task<PipelineAction?> ReadGroupAsync(CancellationToken token);
    }
}
