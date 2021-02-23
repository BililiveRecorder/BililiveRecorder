using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    internal static class StreamExtensions
    {
        // modified from dotnet/runtime 8a52f1e948b6f22f418817ec1068f07b8dae2aa5
        // file: src/libraries/System.Private.CoreLib/src/System/IO/Stream.cs
        // licensed under the MIT license
        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(stream.ReadAsync(array.Array!, array.Offset, array.Count, cancellationToken));
            }

            var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

            static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
            {
                try
                {
                    var result = await readTask.ConfigureAwait(false);
                    new ReadOnlySpan<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                    return result;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(localBuffer);
                }
            }
        }
    }
}
