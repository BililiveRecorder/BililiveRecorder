using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.ToolBox
{
    public interface ICommandHandler<TRequest, TResponse>
        where TRequest : ICommandRequest<TResponse>
        where TResponse : IResponseData
    {
        string Name { get; }
        Task<CommandResponse<TResponse>> Handle(TRequest request, CancellationToken cancellationToken, ProgressCallback? progress);
    }
}
