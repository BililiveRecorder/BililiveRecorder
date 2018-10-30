using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BililiveRecorder.FlvProcessor
{
    // TODO: 重构 Tag 解析流程
    // TODO: 添加测试
    // 注：下载时会按照 4 11 N bytes 下载
    public class FlvStreamProcessor : IFlvStreamProcessor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        internal const uint SEC_TO_MS = 1000; // 1 second = 1000 ms
        internal const int MIN_BUFFER_SIZE = 1024 * 2;
        internal static readonly byte[] FLV_HEADER_BYTES = new byte[]
        {
            0x46, // F
            0x4c, // L
            0x56, // V
            0x01, // Version 1
            0x05, // bit 00000 1 0 1 (have video and audio)
            0x00, // ---
            0x00, //  |
            0x00, //  |
            0x09, // total of 9 bytes
            // 0x00, // ---
            // 0x00, //  |
            // 0x00, //  |
            // 0x00, // the "0th" tag has a length of 0
        };

        private bool _headerParsed = false;
        private readonly List<IFlvTag> HTags = new List<IFlvTag>();
        private readonly List<IFlvTag> Tags = new List<IFlvTag>();
        private readonly MemoryStream _buffer = new MemoryStream();
        private readonly MemoryStream _data = new MemoryStream();
        private FileStream targetFile;
        private IFlvTag currentTag = null;
        private readonly object _writelock = new object();
        private bool Finallized = false;

        private readonly Func<IFlvClipProcessor> funcFlvClipProcessor;
        private readonly Func<byte[], IFlvMetadata> funcFlvMetadata;
        private readonly Func<IFlvTag> funcFlvTag;

        public IFlvMetadata Metadata { get; set; } = null;
        public event TagProcessedEvent TagProcessed;
        public event StreamFinalizedEvent StreamFinalized;
        public Func<string> GetStreamFileName { get; private set; }
        public Func<string> GetClipFileName { get; private set; }

        public ObservableCollection<IFlvClipProcessor> Clips { get; } = new ObservableCollection<IFlvClipProcessor>();

        public EnabledFeature EnabledFeature { get; private set; }

        public uint ClipLengthPast { get; set; } = 90;
        public uint ClipLengthFuture { get; set; } = 30;

        public int LasttimeRemovedTimestamp { get; private set; } = 0;
        public int MaxTimeStamp { get; private set; } = 0;
        public int BaseTimeStamp { get; private set; } = 0;
        public int TagVideoCount { get; private set; } = 0;
        public int TagAudioCount { get; private set; } = 0;
        private bool hasOffset = false;

        public FlvStreamProcessor(Func<IFlvClipProcessor> funcFlvClipProcessor, Func<byte[], IFlvMetadata> funcFlvMetadata, Func<IFlvTag> funcFlvTag)
        {
            this.funcFlvClipProcessor = funcFlvClipProcessor;
            this.funcFlvMetadata = funcFlvMetadata;
            this.funcFlvTag = funcFlvTag;


        }

        public IFlvStreamProcessor Initialize(Func<string> getStreamFileName, Func<string> getClipFileName, EnabledFeature enabledFeature)
        {
            GetStreamFileName = getStreamFileName;
            GetClipFileName = getClipFileName;
            EnabledFeature = enabledFeature;

            if (EnabledFeature.IsRecordEnabled())
            {
                string path = GetStreamFileName();
                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch (Exception) { }
                targetFile = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
            }

            return this;
        }

        public void AddBytes(byte[] data)
        {
            lock (_writelock)
            {
                _AddBytes(data);
            }
        }

        private void _AddBytes(byte[] data)
        {
            if (Finallized)
            {
                throw new Exception("Stream File Already Closed");
            }
            else if (!_headerParsed)
            {
                if (data[4] != FLV_HEADER_BYTES[4])
                {
                    // 七牛 直播云 的高端 FLV 头
                    logger.Debug("FLV头[4]的值是 {0}", data[4]);
                    data[4] = FLV_HEADER_BYTES[4];
                }
                var r = new bool[FLV_HEADER_BYTES.Length];
                for (int i = 0; i < FLV_HEADER_BYTES.Length; i++)
                {
                    r[i] = data[i] == FLV_HEADER_BYTES[i];
                }

                bool succ = r.All(x => x);
                if (!succ)
                {
                    throw new NotSupportedException("Not FLV Stream or Not Supported"); // TODO: custom Exception.
                }

                _headerParsed = true;
                _AddBytes(data.Skip(FLV_HEADER_BYTES.Length).ToArray());
            }
            else if (currentTag == null)
            {
                _buffer.Write(data, 0, data.Length);
                if (_buffer.Position >= MIN_BUFFER_SIZE)
                {
                    _ParseTag(_buffer.GetBuffer().Take((int)_buffer.Position).ToArray());
                }
            }
            else
            {
                _WriteTagData(data);
            }
        }

        private void _WriteTagData(byte[] data)
        {
            int toRead = Math.Min(data.Length, (currentTag.TagSize - (int)_data.Position));
            _data.Write(data, 0, toRead);
            if ((int)_data.Position == currentTag.TagSize)
            {
                currentTag.Data = _data.ToArray();
                _data.SetLength(0); // reset data buffer
                _TagCreated(currentTag);
                currentTag = null;
                _AddBytes(data.Skip(toRead).ToArray());
            }
        }

        private void _TagCreated(IFlvTag tag)
        {
            if (Metadata == null)
            {
                if (tag.TagType == TagType.DATA)
                {
                    targetFile?.Write(FLV_HEADER_BYTES, 0, FLV_HEADER_BYTES.Length);
                    targetFile?.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                    Metadata = funcFlvMetadata(tag.Data);

                    // TODO: 添加录播姬标记、录制信息

                    tag.Data = Metadata.ToBytes();
                    tag.WriteTo(targetFile);
                }
                else
                {
                    throw new Exception("onMetaData not found");
                }
            }
            else
            {
                if (!hasOffset)
                {
                    if (tag.TagType == TagType.VIDEO)
                    {
                        TagVideoCount++;
                        if (TagVideoCount < 2)
                        {
                            logger.Trace("第一个 Video Tag 时间戳 {0} ms", tag.TimeStamp);
                            HTags.Add(tag);
                        }
                        else
                        {
                            BaseTimeStamp = tag.TimeStamp;
                            hasOffset = true;
                            logger.Trace("重设时间戳 {0} 毫秒", BaseTimeStamp);
                        }
                    }
                    else if (tag.TagType == TagType.AUDIO)
                    {
                        TagAudioCount++;
                        if (TagAudioCount < 2)
                        {
                            logger.Trace("第一个 Audio Tag 时间戳 {0} ms", tag.TimeStamp);
                            HTags.Add(tag);
                        }
                        else
                        {
                            BaseTimeStamp = tag.TimeStamp;
                            hasOffset = true;
                            logger.Trace("重设时间戳 {0} 毫秒", BaseTimeStamp);
                        }
                    }
                }


                if (hasOffset)
                {
                    tag.TimeStamp -= BaseTimeStamp; // 修复时间戳
                    if (tag.TimeStamp < 0)
                    {
                        tag.TimeStamp = 0;
                    }

                    MaxTimeStamp = Math.Max(MaxTimeStamp, tag.TimeStamp);
                }
                else
                {
                    tag.TimeStamp = 0;
                }

                // 如果启用了 Clip 功能
                if (EnabledFeature.IsClipEnabled())
                {
                    Tags.Add(tag); // Clip 缓存

                    // 移除过旧的数据
                    if (MaxTimeStamp - LasttimeRemovedTimestamp > 800)
                    {
                        LasttimeRemovedTimestamp = MaxTimeStamp;
                        int max_remove_index = Tags.FindLastIndex(x => x.IsVideoKeyframe && ((MaxTimeStamp - x.TimeStamp) > (ClipLengthPast * SEC_TO_MS)));
                        if (max_remove_index > 0)
                        {
                            Tags.RemoveRange(0, max_remove_index);
                        }
                        // Tags.RemoveRange(0, max_remove_index + 1 - 1);
                        // 给将来的备注：这里是故意 + 1 - 1 的，因为要保留选中的那个关键帧， + 1 就把关键帧删除了
                    }
                }

                // 写入硬盘
                tag.WriteTo(targetFile);

                Clips.ToList().ForEach(fcp => fcp.AddTag(tag));
                TagProcessed?.Invoke(this, new TagProcessedArgs() { Tag = tag });
            } // if (Metadata == null) else
        }

        private void _ParseTag(byte[] data)
        {
            _buffer.Position = 0;
            _buffer.SetLength(0);
            byte[] b = new byte[4];
            _buffer.Write(data, 0, data.Length);
            long dataLen = _buffer.Position;
            _buffer.Position = 0;
            IFlvTag tag = funcFlvTag();

            // Previous Tag Size
            _buffer.Read(b, 0, 4);
            b = new byte[4];

            // TagType UI8
            tag.TagType = (TagType)_buffer.ReadByte();
            // Debug.Write(string.Format("Tag Type: {0}\n", tag.TagType));

            // DataSize UI24
            _buffer.Read(b, 1, 3);
            tag.TagSize = BitConverter.ToInt32(b.ToBE(), 0);

            // Timestamp UI24
            _buffer.Read(b, 1, 3);
            // TimestampExtended UI8
            _buffer.Read(b, 0, 1);
            tag.TimeStamp = BitConverter.ToInt32(b.ToBE(), 0);

            // StreamID UI24
            _buffer.Read(tag.StreamId, 0, 3);

            currentTag = tag;
            byte[] rest = _buffer.GetBuffer().Skip((int)_buffer.Position).Take((int)(dataLen - _buffer.Position)).ToArray();
            _buffer.Position = 0;

            _AddBytes(rest);
        }

        public IFlvClipProcessor Clip()
        {
            // 如果禁用 clip 功能 或者 已经结束处理了
            if (!EnabledFeature.IsClipEnabled() || Finallized)
            {
                return null;
            }
            else
            {
                lock (_writelock)
                {
                    logger.Info("剪辑处理中，将会保存过去 {0} 秒和将来 {1} 秒的直播流", (Tags[Tags.Count - 1].TimeStamp - Tags[0].TimeStamp) / 1000d, ClipLengthFuture);
                    IFlvClipProcessor clip = funcFlvClipProcessor().Initialize(GetClipFileName(), Metadata, HTags, new List<IFlvTag>(Tags.ToArray()), ClipLengthFuture);
                    Clips.Add(clip);
                    return clip;
                }
            }
        }

        public void FinallizeFile()
        {
            if (!Finallized)
            {
                lock (_writelock)
                {
                    try
                    {
                        Metadata.Meta["duration"] = MaxTimeStamp / 1000.0;
                        Metadata.Meta["lasttimestamp"] = (double)MaxTimeStamp;
                        byte[] metadata = Metadata.ToBytes();


                        // 13 for FLV header & "0th" tag size
                        // 11 for 1st tag header
                        targetFile?.Seek(13 + 11, SeekOrigin.Begin);
                        targetFile?.Write(metadata, 0, metadata.Length);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "保存录制文件时出错");
                    }
                    finally
                    {
                        targetFile?.Close();
                        _buffer.Close();
                        _data.Close();
                        Tags.Clear();

                        Finallized = true;

                        Clips.ToList().ForEach(fcp => fcp.FinallizeFile());
                        StreamFinalized?.Invoke(this, new StreamFinalizedArgs() { StreamProcessor = this });
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _buffer.Dispose();
                    _data.Dispose();
                    targetFile?.Dispose();
                }
                Tags.Clear();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
