using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv
{
    public static class TagExtentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsScript(this Tag tag)
            => tag.Type == TagType.Script;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsHeader(this Tag tag)
            => (0 != (tag.Flag & TagFlag.Header))
            && (tag.Type == TagType.Video || tag.Type == TagType.Audio);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnd(this Tag tag)
            => 0 != (tag.Flag & TagFlag.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsData(this Tag tag)
            => (0 == (tag.Flag & (TagFlag.Header | TagFlag.End)))
            && (tag.Type == TagType.Video || tag.Type == TagType.Audio);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNonKeyframeData(this Tag tag)
            => (tag.Type == TagType.Video || tag.Type == TagType.Audio)
            && tag.Flag == TagFlag.None;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsKeyframeData(this Tag tag)
            => tag.Type == TagType.Video && tag.Flag == TagFlag.Keyframe;

        public static async Task WriteTo(this Tag tag, Stream target, int timestamp, IMemoryStreamProvider? memoryStreamProvider = null)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(11);
            var dispose = true;
            Stream? data = null;
            try
            {
                if (tag.IsScript())
                {
                    if (tag.ScriptData is null)
                        throw new Exception("BinaryData is null");

                    data = memoryStreamProvider?.CreateMemoryStream(nameof(TagExtentions) + ":" + nameof(WriteTo) + ":TagBodyTemp") ?? new MemoryStream();
                    tag.ScriptData.WriteTo(data);
                }
                else if (tag.Nalus != null)
                {
                    if (tag.BinaryData is null)
                        throw new Exception("BinaryData is null");

                    data = memoryStreamProvider?.CreateMemoryStream(nameof(TagExtentions) + ":" + nameof(WriteTo) + ":TagBodyTemp") ?? new MemoryStream();

                    tag.BinaryData.Seek(0, SeekOrigin.Begin);

                    tag.BinaryData.Read(buffer, 0, 5);
                    data.Write(buffer, 0, 5);

                    foreach (var nalu in tag.Nalus)
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 0, 4), nalu.FullSize);
                        data.Write(buffer, 0, 4);

                        tag.BinaryData.Seek(nalu.StartPosition, SeekOrigin.Begin);
                        await tag.BinaryData.CopyBytesAsync(data, nalu.FullSize).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (tag.BinaryData is null)
                        throw new Exception("BinaryData is null");

                    dispose = false;
                    data = tag.BinaryData;
                }

                data.Seek(0, SeekOrigin.Begin);

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 0, 4), (uint)data.Length);
                buffer[0] = (byte)tag.Type;

                unsafe
                {
                    var stackTemp = stackalloc byte[4];
                    BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(stackTemp, 4), timestamp);
                    buffer[4] = stackTemp[1];
                    buffer[5] = stackTemp[2];
                    buffer[6] = stackTemp[3];
                    buffer[7] = stackTemp[0];
                }

                buffer[8] = 0;
                buffer[9] = 0;
                buffer[10] = 0;

                await target.WriteAsync(buffer, 0, 11).ConfigureAwait(false);
                await data.CopyToAsync(target).ConfigureAwait(false);

                BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(buffer, 0, 4), (uint)data.Length + 11);
                await target.WriteAsync(buffer, 0, 4).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                if (dispose)
                    data?.Dispose();
            }
        }
    }
}
