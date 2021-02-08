using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline
{
    public interface IFullProcessingRule : IProcessingRule
    {
        Task RunAsync(FlvProcessingContext context, ProcessingDelegate next);
    }
}
