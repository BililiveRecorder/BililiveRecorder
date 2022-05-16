using System;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Danmaku;

namespace BililiveRecorder.Core.Api
{
    internal interface IDanmakuClient : IDisposable
    {
        bool Connected { get; }

        event EventHandler<StatusChangedEventArgs>? StatusChanged;
        event EventHandler<DanmakuReceivedEventArgs>? DanmakuReceived;

        Task ConnectAsync(int roomid, CancellationToken cancellationToken);
        Task DisconnectAsync();
    }
}
