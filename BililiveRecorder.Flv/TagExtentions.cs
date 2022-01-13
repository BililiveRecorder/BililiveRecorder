using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FastHashes;

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
                // 先准备 Tag 的 body 部分
                if (tag.IsScript())
                {
                    // Script Tag 写入时使用 Script Tag 序列化
                    if (tag.ScriptData is null)
                        throw new Exception("ScriptData is null");

                    data = memoryStreamProvider?.CreateMemoryStream(nameof(TagExtentions) + ":" + nameof(WriteTo) + ":TagBodyTemp") ?? new MemoryStream();
                    tag.ScriptData.WriteTo(data);
                }
                else if (tag.Nalus != null)
                {
                    // 如果 Tag 有 Nalu 信息，则按照 Nalus 里面的指示分段复制
                    // 这个 Tag 一定是 Video
                    if (tag.BinaryData is null)
                        throw new Exception("BinaryData is null");

                    data = memoryStreamProvider?.CreateMemoryStream(nameof(TagExtentions) + ":" + nameof(WriteTo) + ":TagBodyTemp") ?? new MemoryStream();

                    tag.BinaryData.Seek(0, SeekOrigin.Begin);
                    tag.BinaryData.Read(buffer, 0, 5);

                    if (tag.ExtraData is not null)
                    {
                        // 如果有 ExtraData 则以这里面的 composition time 为准
                        Int24.WriteInt24(buffer.AsSpan(2, 3), tag.ExtraData.CompositionTime);
                    }

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

                    if (tag.Type == TagType.Video && tag.ExtraData is not null)
                    {
                        // 复制并修改 composition time
                        data = memoryStreamProvider?.CreateMemoryStream(nameof(TagExtentions) + ":" + nameof(WriteTo) + ":TagBodyTemp") ?? new MemoryStream();
                        tag.BinaryData.CopyTo(data);

                        Int24.WriteInt24(buffer.AsSpan(0, 3), tag.ExtraData.CompositionTime);
                        data.Seek(2, SeekOrigin.Begin);
                        data.Read(buffer, 0, 3);
                    }
                    else
                    {
                        // 直接复用原数据
                        dispose = false;
                        data = tag.BinaryData;
                    }
                }

                data.Seek(0, SeekOrigin.Begin);

                // 序列号 Tag 的 header 部分
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

                // 写入
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

        public static TagExtraData? UpdateExtraData(this Tag tag)
        {
            if (tag.BinaryData is not { } binaryData || binaryData.Length < 5)
            {
                tag.ExtraData = null;
            }
            else
            {
                var old_position = binaryData.Position;
                var extra = new TagExtraData();

                binaryData.Position = 0;

                var buffer = ArrayPool<byte>.Shared.Rent(5);
                try
                {
                    binaryData.Read(buffer, 0, 5);
                    extra.FirstBytes = BinaryConvertUtilities.ByteArrayToHexString(buffer, 0, 2);

                    if (tag.Type == TagType.Video)
                    {
                        extra.CompositionTime = Int24.ReadInt24(buffer.AsSpan(2, 3));
                        extra.FinalTime = tag.Timestamp + extra.CompositionTime;
                    }
                    else
                    {
                        extra.CompositionTime = int.MinValue;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }

                binaryData.Position = old_position;
                tag.ExtraData = extra;
            }
            return tag.ExtraData;
        }

        private static readonly FarmHash64 farmHash64 = new();

        public static string? UpdateDataHash(this Tag tag)
        {
            if (tag.BinaryData is null)
            {
                tag.DataHash = null;
            }
            else
            {
                var buffer = tag.BinaryData.GetBuffer();
                tag.DataHash = BinaryConvertUtilities.ByteArrayToHexString(farmHash64.ComputeHash(buffer, (int)tag.BinaryData.Length));

                if (tag.Nalus?.Count > 0)
                {
                    foreach (var nalu in tag.Nalus)
                    {
                        nalu.NaluHash = BinaryConvertUtilities.ByteArrayToHexString(farmHash64.ComputeHash(buffer, nalu.StartPosition, (int)nalu.FullSize));
                    }
                }
            }
            return tag.DataHash;
        }
    }
}
