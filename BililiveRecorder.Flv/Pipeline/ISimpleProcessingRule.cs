using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline
{
    public interface ISimpleProcessingRule : IProcessingRule
    {
        Task RunAsync(FlvProcessingContext context, Func<Task> next);
    }
}
