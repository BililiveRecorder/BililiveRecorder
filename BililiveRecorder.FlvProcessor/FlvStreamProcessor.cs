using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace BililiveRecorder.FlvProcessor
{
    // TODO: 添加测试
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


        private readonly object _writelock = new object();
        private readonly List<IFlvTag> _headerTags = new List<IFlvTag>();
        private readonly List<IFlvTag> _tags = new List<IFlvTag>();
        private readonly MemoryStream _data = new MemoryStream();
        private FileStream _targetFile;
        private IFlvTag _currentTag = null;
        private byte[] _leftover = null;
        private bool _finalized = false;
        private bool _headerParsed = false;
        private bool _hasOffset = false;
        private int _lasttimeRemovedTimestamp = 0;
        private int _baseTimeStamp = 0;
        private int _writeTimeStamp = 0;
        private int _tagVideoCount = 0;
        private int _tagAudioCount = 0;

        public int TotalMaxTimestamp { get; private set; } = 0;
        public int CurrentMaxTimestamp { get => TotalMaxTimestamp - _writeTimeStamp; }
        public DateTime StartDateTime { get; private set; } = DateTime.Now;

        private readonly Func<IFlvClipProcessor> funcFlvClipProcessor;
        private readonly Func<byte[], IFlvMetadata> funcFlvMetadata;
        private readonly Func<IFlvTag> funcFlvTag;

        private Func<string> GetStreamFileName;
        private Func<string> GetClipFileName;

        public event TagProcessedEvent TagProcessed;
        public event StreamFinalizedEvent StreamFinalized;

        public uint ClipLengthPast { get; set; } = 20;
        public uint ClipLengthFuture { get; set; } = 10;
        public uint CuttingNumber { get; set; } = 10;
        private EnabledFeature EnabledFeature;
        private AutoCuttingMode CuttingMode;

        public IFlvMetadata Metadata { get; set; } = null;
        public ObservableCollection<IFlvClipProcessor> Clips { get; } = new ObservableCollection<IFlvClipProcessor>();

        public FlvStreamProcessor(Func<IFlvClipProcessor> funcFlvClipProcessor, Func<byte[], IFlvMetadata> funcFlvMetadata, Func<IFlvTag> funcFlvTag)
        {
            this.funcFlvClipProcessor = funcFlvClipProcessor;
            this.funcFlvMetadata = funcFlvMetadata;
            this.funcFlvTag = funcFlvTag;


        }

        public IFlvStreamProcessor Initialize(Func<string> getStreamFileName, Func<string> getClipFileName, EnabledFeature enabledFeature, AutoCuttingMode autoCuttingMode)
        {
            GetStreamFileName = getStreamFileName;
            GetClipFileName = getClipFileName;
            EnabledFeature = enabledFeature;
            CuttingMode = autoCuttingMode;

            if (EnabledFeature.IsRecordEnabled()) { OpenNewRecordFile(); }

            return this;
        }

        private void OpenNewRecordFile()
        {
            string path = GetStreamFileName();
            logger.Debug("打开新录制文件: " + path);
            try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch (Exception) { }
            _targetFile = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);

            if (_headerParsed)
            {
                _targetFile.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                _targetFile.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                new FlvTag
                {
                    TagType = TagType.DATA,
                    Data = Metadata.ToBytes()
                }.WriteTo(_targetFile);

                _headerTags.ForEach(tag => tag.WriteTo(_targetFile));
            }
        }

        public void AddBytes(byte[] data)
        {
            lock (_writelock)
            {
                if (_finalized) { throw new InvalidOperationException("Processor Already Closed"); }
                if (_leftover != null)
                {
                    byte[] c = new byte[_leftover.Length + data.Length];
                    _leftover.CopyTo(c, 0);
                    data.CopyTo(c, _leftover.Length);
                    _leftover = null;
                    ParseBytes(c);
                }
                else
                {
                    ParseBytes(data);
                }
            }
        }

        private void ParseBytes(byte[] data)
        {
            int position = 0;
            if (!_headerParsed) { ReadFlvHeader(); }
            while (position < data.Length)
            {
                if (_currentTag == null)
                {
                    if (!ParseTagHead())
                    {
                        _leftover = data.Skip(position).ToArray();
                        break;
                    }
                }
                else
                { FillTagData(); }
            }
            bool ParseTagHead()
            {
                if (data.Length - position < 15) { return false; }

                byte[] b = new byte[4];
                IFlvTag tag = funcFlvTag();

                // Previous Tag Size UI24
                position += 4;

                // TagType UI8
                tag.TagType = (TagType)data[position++];

                // DataSize UI24
                b[1] = data[position++];
                b[2] = data[position++];
                b[3] = data[position++];
                tag.TagSize = BitConverter.ToInt32(b.ToBE(), 0);

                // Timestamp UI24
                b[1] = data[position++];
                b[2] = data[position++];
                b[3] = data[position++];
                // TimestampExtended UI8
                b[0] = data[position++];
                tag.SetTimeStamp(BitConverter.ToInt32(b.ToBE(), 0));

                // StreamID UI24
                tag.StreamId[0] = data[position++];
                tag.StreamId[1] = data[position++];
                tag.StreamId[2] = data[position++];

                _currentTag = tag;

                return true;
            }
            void FillTagData()
            {
                int toRead = Math.Min(data.Length - position, _currentTag.TagSize - (int)_data.Position);
                _data.Write(buffer: data, offset: position, count: toRead);
                position += toRead;
                if ((int)_data.Position == _currentTag.TagSize)
                {
                    _currentTag.Data = _data.ToArray();
                    _data.SetLength(0); // reset data buffer
                    TagCreated(_currentTag);
                    _currentTag = null;
                }
            }
            void ReadFlvHeader()
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
                position += FLV_HEADER_BYTES.Length;
            }
        }

        private void TagCreated(IFlvTag tag)
        {
            if (Metadata == null)
            {
                ParseMetadata();
            }
            else
            {
                if (!_hasOffset) { ParseTimestampOffset(); }
                SetTimestamp();

                if (EnabledFeature.IsRecordEnabled()) { ProcessRecordLogic(); }
                if (EnabledFeature.IsClipEnabled()) { ProcessClipLogic(); }

                TagProcessed?.Invoke(this, new TagProcessedArgs() { Tag = tag });
            }
            return;
            void SetTimestamp()
            {
                if (_hasOffset)
                {
                    tag.SetTimeStamp(Math.Max(0, tag.TimeStamp - _baseTimeStamp)); // 修复时间戳
                    TotalMaxTimestamp = Math.Max(TotalMaxTimestamp, tag.TimeStamp);
                }
                else
                { tag.SetTimeStamp(0); }
            }
            void ProcessRecordLogic()
            {
                if (CuttingMode != AutoCuttingMode.Disabled && tag.IsVideoKeyframe)
                {
                    bool byTime = (CuttingMode == AutoCuttingMode.ByTime) && (CurrentMaxTimestamp / 1000 >= CuttingNumber * 60);
                    bool bySize = (CuttingMode == AutoCuttingMode.BySize) && ((_targetFile.Length / 1024 / 1024) >= CuttingNumber);
                    if (byTime || bySize)
                    {
                        FinallizeCurrentFile();
                        OpenNewRecordFile();
                        _writeTimeStamp = TotalMaxTimestamp;
                    }
                }

                tag.WriteTo(_targetFile, _writeTimeStamp);
            }
            void ProcessClipLogic()
            {
                _tags.Add(tag);

                // 移除过旧的数据
                if (TotalMaxTimestamp - _lasttimeRemovedTimestamp > 800)
                {
                    _lasttimeRemovedTimestamp = TotalMaxTimestamp;
                    int max_remove_index = _tags.FindLastIndex(x => x.IsVideoKeyframe && ((TotalMaxTimestamp - x.TimeStamp) > (ClipLengthPast * SEC_TO_MS)));
                    if (max_remove_index > 0)
                    {
                        _tags.RemoveRange(0, max_remove_index);
                    }
                    // Tags.RemoveRange(0, max_remove_index + 1 - 1);
                    // 给将来的备注：这里是故意 + 1 - 1 的，因为要保留选中的那个关键帧， + 1 就把关键帧删除了
                }

                Clips.ToList().ForEach(fcp => fcp.AddTag(tag));
            }
            void ParseTimestampOffset()
            {
                if (tag.TagType == TagType.VIDEO)
                {
                    _tagVideoCount++;
                    if (_tagVideoCount < 2)
                    {
                        logger.Trace("第一个 Video Tag 时间戳 {0} ms", tag.TimeStamp);
                        _headerTags.Add(tag);
                    }
                    else
                    {
                        _baseTimeStamp = tag.TimeStamp;
                        _hasOffset = true;
                        StartDateTime = DateTime.Now;
                        logger.Trace("重设时间戳 {0} 毫秒", _baseTimeStamp);
                    }
                }
                else if (tag.TagType == TagType.AUDIO)
                {
                    _tagAudioCount++;
                    if (_tagAudioCount < 2)
                    {
                        logger.Trace("第一个 Audio Tag 时间戳 {0} ms", tag.TimeStamp);
                        _headerTags.Add(tag);
                    }
                    else
                    {
                        _baseTimeStamp = tag.TimeStamp;
                        _hasOffset = true;
                        StartDateTime = DateTime.Now;
                        logger.Trace("重设时间戳 {0} 毫秒", _baseTimeStamp);
                    }
                }
            }
            void ParseMetadata()
            {
                if (tag.TagType == TagType.DATA)
                {
                    _targetFile?.Write(FLV_HEADER_BYTES, 0, FLV_HEADER_BYTES.Length);
                    _targetFile?.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                    Metadata = funcFlvMetadata(tag.Data);

                    // TODO: 添加录播姬标记、录制信息

                    tag.Data = Metadata.ToBytes();
                    tag.WriteTo(_targetFile);
                }
                else
                {
                    throw new Exception("onMetaData not found");
                }
            }
        }

        public IFlvClipProcessor Clip()
        {
            if (!EnabledFeature.IsClipEnabled()) { return null; }
            lock (_writelock)
            {
                if (_finalized) { throw new InvalidOperationException("Processor Already Closed"); }

                logger.Info("剪辑处理中，将会保存过去 {0} 秒和将来 {1} 秒的直播流", (_tags[_tags.Count - 1].TimeStamp - _tags[0].TimeStamp) / 1000d, ClipLengthFuture);
                IFlvClipProcessor clip = funcFlvClipProcessor().Initialize(GetClipFileName(), Metadata, _headerTags, new List<IFlvTag>(_tags.ToArray()), ClipLengthFuture);
                clip.ClipFinalized += (sender, e) => { Clips.Remove(e.ClipProcessor); };
                Clips.Add(clip);
                return clip;
            }
        }

        private void FinallizeCurrentFile()
        {
            try
            {
                logger.Debug("正在关闭当前录制文件: " + _targetFile.Name);
                Metadata.Meta["duration"] = CurrentMaxTimestamp / 1000.0;
                Metadata.Meta["lasttimestamp"] = (double)CurrentMaxTimestamp;
                byte[] metadata = Metadata.ToBytes();

                // 13 for FLV header & "0th" tag size
                // 11 for 1st tag header
                _targetFile?.Seek(13 + 11, SeekOrigin.Begin);
                _targetFile?.Write(metadata, 0, metadata.Length);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存录制文件时出错");
            }
            finally
            {
                _targetFile?.Close();
                _targetFile = null;
            }
        }

        public void FinallizeFile()
        {
            if (!_finalized)
            {
                lock (_writelock)
                {
                    try
                    {
                        FinallizeCurrentFile();
                    }
                    finally
                    {
                        _targetFile?.Close();
                        _data.Close();
                        _tags.Clear();

                        _finalized = true;

                        Clips.ToList().ForEach(fcp => fcp.FinallizeFile()); // TODO: check
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
                    _data.Dispose();
                    _targetFile?.Dispose();
                }
                _tags.Clear();
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
