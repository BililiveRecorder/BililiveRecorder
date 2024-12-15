using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace BililiveRecorder.Flv.Parser
{
    /// <summary>
    /// 从 <see cref="PipeReader"/> 读取 <see cref="FlvDataTag"/>
    /// </summary>
    public class FlvTagPipeReader : IFlvTagReader, IDisposable
    {
        private static int memoryCreateCounter = 0;

        private readonly ILogger? logger;
        private readonly IMemoryStreamProvider memoryStreamProvider;
        private readonly bool skipData;
        private readonly bool leaveOpen;

        private int tagIndex;

        private bool peek = false;
        private Tag? peekTag = null;
        private readonly SemaphoreSlim peekSemaphoreSlim = new SemaphoreSlim(1, 1);

        private bool fileHeader = false;

        public PipeReader Reader { get; }

        public FlvTagPipeReader(PipeReader reader, IMemoryStreamProvider memoryStreamProvider, ILogger? logger = null) : this(reader, memoryStreamProvider, false, logger) { }

        public FlvTagPipeReader(PipeReader reader, IMemoryStreamProvider memoryStreamProvider, bool skipData = false, ILogger? logger = null) : this(reader, memoryStreamProvider, skipData, false, logger) { }

        public FlvTagPipeReader(PipeReader reader, IMemoryStreamProvider memoryStreamProvider, bool skipData = false, bool leaveOpen = false, ILogger? logger = null)
        {
            this.logger = logger?.ForContext<FlvTagPipeReader>();
            this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));

            this.memoryStreamProvider = memoryStreamProvider ?? throw new ArgumentNullException(nameof(memoryStreamProvider));
            this.skipData = skipData;
            this.leaveOpen = leaveOpen;
        }

        public void Dispose()
        {
            if (!this.leaveOpen)
                this.Reader.Complete();
        }

        /// <summary>
        /// 实现二进制数据的解析
        /// </summary>
        /// <returns>解析出的 Flv Tag</returns>
        private async Task<Tag?> ReadNextTagAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var result = await this.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                var buffer = result.Buffer;

                // In the event that no message is parsed successfully, mark consumed
                // as nothing and examined as the entire buffer.
                var consumed = buffer.Start;
                var examined = buffer.End;

                try
                {
                    if (!this.fileHeader)
                    {
                        if (this.ParseFileHeader(ref buffer))
                        {
                            this.fileHeader = true;
                            consumed = buffer.Start;
                            examined = consumed;
                        }
                        else
                            continue;
                    }

                    if (this.ParseTagData(ref buffer, out var tag))
                    {
                        // A single message was successfully parsed so mark the start as the
                        // parsed buffer as consumed. TryParseMessage trims the buffer to
                        // point to the data after the message was parsed.
                        consumed = buffer.Start;

                        // Examined is marked the same as consumed here, so the next call
                        // to ReadSingleMessageAsync will process the next message if there's
                        // one.
                        examined = consumed;

                        return tag;
                    }
                    else
                    {
                        examined = buffer.End;
                    }

                    if (result.IsCompleted)
                    {
                        //if (buffer.Length > 0)
                        //{
                        //// The message is incomplete and there's no more data to process.
                        //// throw new FlvException("Incomplete message.");
                        //}

                        break;
                    }
                }
                finally
                {
                    this.Reader.AdvanceTo(consumed, examined);
                }
            }

            return null;
        }

        private unsafe bool ParseFileHeader(ref ReadOnlySequence<byte> buffer)
        {
            if (buffer.Length < 9)
                return false;

            var fileHeaderSlice = buffer.Slice(buffer.Start, 9);

            Span<byte> stackSpan = stackalloc byte[9];
            ReadOnlySpan<byte> data = stackSpan;

            if (fileHeaderSlice.IsSingleSegment)
                data = fileHeaderSlice.First.Span;
            else
                fileHeaderSlice.CopyTo(stackSpan);

            if (data[0] != 'F' || data[1] != 'L' || data[2] != 'V' || data[3] != 1)
                throw new NotFlvFileException("Data is not FLV.");

            if (data[5] != 0 || data[6] != 0 || data[7] != 0 || data[8] != 9)
                throw new NotFlvFileException("Not Supported FLV format.");

            buffer = buffer.Slice(fileHeaderSlice.End);

            return true;
        }

        private unsafe bool ParseTagData(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out Tag? tag)
        {
            tag = default;

            if (buffer.Length < 11 + 4)
                return false;

            // Slice Tag Header
            var tagHeaderSlice = buffer.Slice(4, 11);
            buffer = buffer.Slice(tagHeaderSlice.End);

            Span<byte> stackTemp = stackalloc byte[4];
            Span<byte> stackHeaderSpan = stackalloc byte[11];
            ReadOnlySpan<byte> header = stackHeaderSpan;

            if (tagHeaderSlice.IsSingleSegment)
                header = tagHeaderSlice.First.Span;
            else
                tagHeaderSlice.CopyTo(stackHeaderSpan);

            Debug.Assert(header.Length == 11, "Tag header length is not 11.");

            // Read Tag Type
            var tagType = (TagType)header[0];

            switch (tagType)
            {
                case TagType.Audio:
                case TagType.Video:
                case TagType.Script:
                    break;
                default:
                    throw new UnknownFlvTagTypeException("Unknown Tag Type: " + header[0]);
            }

            // Read Tag Size
            stackTemp[0] = 0;
            stackTemp[1] = header[1];
            stackTemp[2] = header[2];
            stackTemp[3] = header[3];
            var tagSize = BinaryPrimitives.ReadUInt32BigEndian(stackTemp);

            // if not enough data are available
            if (buffer.Length < tagSize)
                return false;

            // Read Tag Timestamp
            stackTemp[1] = header[4];
            stackTemp[2] = header[5];
            stackTemp[3] = header[6];
            stackTemp[0] = header[7];
            var tagTimestamp = BinaryPrimitives.ReadInt32BigEndian(stackTemp);

            // Slice Tag Data
            var tagDataSlice = buffer.Slice(buffer.Start, tagSize);
            buffer = buffer.Slice(tagDataSlice.End);

            // Copy Tag Data If Required
            var tagBodyStream = this.memoryStreamProvider.CreateMemoryStream(nameof(FlvTagPipeReader) + ":TagBody:" + Interlocked.Increment(ref memoryCreateCounter));

            foreach (var segment in tagDataSlice)
            {
                var sharedBuffer = ArrayPool<byte>.Shared.Rent(segment.Length);
                try
                {
                    segment.CopyTo(sharedBuffer);
                    tagBodyStream.Write(sharedBuffer, 0, segment.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(sharedBuffer);
                }
            }

            // Parse Tag Flag
            var tagFlag = TagFlag.None;
            var isAVC = true;

            if (tagBodyStream.Length > 2)
            {
                _ = tagBodyStream.Seek(0, SeekOrigin.Begin);
                switch (tagType)
                {
                    case TagType.Audio:
                        {
                            var format = tagBodyStream.ReadByte() >> 4;
                            if (format != 10) // AAC
                                break;
                            var packet = tagBodyStream.ReadByte();
                            if (packet == 0)
                                tagFlag = TagFlag.Header;
                            break;
                        }
                    case TagType.Video:
                        {
                            var frame = tagBodyStream.ReadByte();

                            isAVC = (frame & 0x0F) == 7;
                            // if (!isAVC) throw new UnsupportedCodecException(string.Format("Unsupported Video Codec: {0}.", frame & 0x0F));

                            if ((frame & 0xF0) == 0x10)
                                tagFlag |= TagFlag.Keyframe;

                            var packet = tagBodyStream.ReadByte();
                            tagFlag |= packet switch
                            {
                                0 => TagFlag.Header,
                                2 => TagFlag.End,
                                _ => TagFlag.None,
                            };
                            break;
                        }
                    default:
                        break;
                }
            }

            // Create Tag Object
            tag = new Tag
            {
                Type = tagType,
                Flag = tagFlag,
                Index = Interlocked.Increment(ref this.tagIndex),
                Size = tagSize,
                Timestamp = tagTimestamp,
            };

            // Read Tag Type Specific Data
            _ = tagBodyStream.Seek(0, SeekOrigin.Begin);

            if (tag.Type == TagType.Script)
            {
                try
                {
                    tag.ScriptData = Amf.ScriptTagBody.Parse(tagBodyStream);
                }
                catch (Exception ex)
                {
                    this.logger?.Debug(ex, "Error parsing script tag body");
                }
            }
            else if (isAVC && tag.Type == TagType.Video && (0 == (tag.Flag & TagFlag.Header)))
            {
                if (H264Nalu.TryParseNalu(tagBodyStream, out var nalus))
                    tag.Nalus = nalus;
            }

            // Dispose Stream If Not Needed
            if (!this.skipData || tag.ShouldSerializeBinaryDataForSerializationUseOnly())
                tag.BinaryData = tagBodyStream;
            else
                tagBodyStream.Dispose();

            return true;
        }

        /// <inheritdoc/>
        public async Task<Tag?> PeekTagAsync(CancellationToken token)
        {
            try
            {
                this.peekSemaphoreSlim.Wait();

                if (this.peek)
                {
                    return this.peekTag;
                }
                else
                {
                    this.peekTag = await this.ReadNextTagAsync(token);
                    this.peek = true;
                    return this.peekTag;
                }
            }
            finally
            {
                _ = this.peekSemaphoreSlim.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Tag?> ReadTagAsync(CancellationToken token)
        {
            try
            {
                this.peekSemaphoreSlim.Wait();
                if (this.peek)
                {
                    var tag = this.peekTag;
                    this.peekTag = null;
                    this.peek = false;
                    return tag;
                }
                else
                {
                    return await this.ReadNextTagAsync(token);
                }
            }
            finally
            {
                _ = this.peekSemaphoreSlim.Release();
            }
        }
    }
}
