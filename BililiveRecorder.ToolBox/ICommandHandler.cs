using System.Threading.Tasks;

namespace BililiveRecorder.ToolBox
{
    public interface ICommandHandler<TRequest, TResponse> where TRequest : ICommandRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request);
        void PrintResponse(TResponse response);
    }
}
