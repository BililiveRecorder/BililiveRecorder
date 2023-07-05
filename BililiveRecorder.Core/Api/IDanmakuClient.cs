using System;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Api.Danmaku;
using BililiveRecorder.Core.Config;

namespace BililiveRecorder.Core.Api
{
    internal interface IDanmakuClient : IDisposable
    {
        bool Connected { get; }

        event EventHandler<StatusChangedEventArgs>? StatusChanged;
        event EventHandler<DanmakuReceivedEventArgs>? DanmakuReceived;

        Func<string, string?>? BeforeHandshake { get; set; }

        Task ConnectAsync(int roomid, DanmakuTransportMode transportMode, CancellationToken cancellationToken);
        Task DisconnectAsync();
    }
}
