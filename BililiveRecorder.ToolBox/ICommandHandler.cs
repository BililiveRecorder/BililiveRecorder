using System.Threading.Tasks;

namespace BililiveRecorder.ToolBox
{
    public interface ICommandHandler<TRequest, TResponse>
        where TRequest : ICommandRequest<TResponse>
        where TResponse : class
    {
        Task<CommandResponse<TResponse>> Handle(TRequest request);
        void PrintResponse(TResponse response);
    }
}
