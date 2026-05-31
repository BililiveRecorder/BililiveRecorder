using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal interface IDanmakuTransport : IDisposable
    {
        Task<PipeReader> ConnectAsync(string host, int port, AllowedAddressFamily allowedAddressFamily, CancellationToken cancellationToken);
        Task SendAsync(byte[] buffer, int offset, int count);
    }
}
