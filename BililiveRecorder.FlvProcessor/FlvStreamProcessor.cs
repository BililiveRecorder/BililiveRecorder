using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NLog;

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
        public int CurrentMaxTimestamp { get => this.TotalMaxTimestamp - this._writeTimeStamp; }

        private readonly Func<IFlvClipProcessor> funcFlvClipProcessor;
        private readonly Func<byte[], IFlvMetadata> funcFlvMetadata;
        private readonly Func<IFlvTag> funcFlvTag;

        private Func<(string fullPath, string relativePath)> GetStreamFileName;
        private Func<string> GetClipFileName;

        public event TagProcessedEvent TagProcessed;
        public event EventHandler<long> FileFinalized;
        public event StreamFinalizedEvent StreamFinalized;
        public event FlvMetadataEvent OnMetaData;

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

        public IFlvStreamProcessor Initialize(Func<(string fullPath, string relativePath)> getStreamFileName, Func<string> getClipFileName, EnabledFeature enabledFeature, AutoCuttingMode autoCuttingMode)
        {
            this.GetStreamFileName = getStreamFileName;
            this.GetClipFileName = getClipFileName;
            this.EnabledFeature = enabledFeature;
            this.CuttingMode = autoCuttingMode;

            return this;
        }

        private void OpenNewRecordFile()
        {
            var (fullPath, relativePath) = this.GetStreamFileName();
            logger.Debug("打开新录制文件: " + fullPath);
            try { Directory.CreateDirectory(Path.GetDirectoryName(fullPath)); } catch (Exception) { }
            this._targetFile = new FileStream(fullPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);

            if (this._headerParsed)
            {
                this._targetFile.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                this._targetFile.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                var script_tag = this.funcFlvTag();
                script_tag.TagType = TagType.DATA;
                if (this.Metadata.ContainsKey("BililiveRecorder"))
                {
                    // TODO: 更好的写法
                    (this.Metadata["BililiveRecorder"] as Dictionary<string, object>)["starttime"] = DateTime.UtcNow;
                }
                script_tag.Data = this.Metadata.ToBytes();
                script_tag.WriteTo(this._targetFile);

                this._headerTags.ForEach(tag => tag.WriteTo(this._targetFile));
            }
        }

        public void AddBytes(byte[] data)
        {
            lock (this._writelock)
            {
                if (this._finalized) { return; /*throw new InvalidOperationException("Processor Already Closed");*/ }
                if (this._leftover != null)
                {
                    byte[] c = new byte[this._leftover.Length + data.Length];
                    this._leftover.CopyTo(c, 0);
                    data.CopyTo(c, this._leftover.Length);
                    this._leftover = null;
                    this.ParseBytes(c);
                }
                else
                {
                    this.ParseBytes(data);
                }
            }
        }

        private void ParseBytes(byte[] data)
        {
            int position = 0;
            if (!this._headerParsed) { ReadFlvHeader(); }
            while (position < data.Length)
            {
                if (this._currentTag == null)
                {
                    if (!ParseTagHead())
                    {
                        this._leftover = data.Skip(position).ToArray();
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
                IFlvTag tag = this.funcFlvTag();

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

                this._currentTag = tag;

                return true;
            }
            void FillTagData()
            {
                int toRead = Math.Min(data.Length - position, this._currentTag.TagSize - (int)this._data.Position);
                this._data.Write(buffer: data, offset: position, count: toRead);
                position += toRead;
                if ((int)this._data.Position == this._currentTag.TagSize)
                {
                    this._currentTag.Data = this._data.ToArray();
                    this._data.SetLength(0); // reset data buffer
                    this.TagCreated(this._currentTag);
                    this._currentTag = null;
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

                this._headerParsed = true;
                position += FLV_HEADER_BYTES.Length;
            }
        }

        private void TagCreated(IFlvTag tag)
        {
            if (this.Metadata == null)
            {
                ParseMetadata();
            }
            else
            {
                if (!this._hasOffset) { ParseTimestampOffset(); }
                SetTimestamp();

                if (this.EnabledFeature.IsRecordEnabled()) { ProcessRecordLogic(); }
                if (this.EnabledFeature.IsClipEnabled()) { ProcessClipLogic(); }

                TagProcessed?.Invoke(this, new TagProcessedArgs() { Tag = tag });
            }
            return;
            void SetTimestamp()
            {
                if (this._hasOffset)
                {
                    tag.SetTimeStamp(tag.TimeStamp - this._baseTimeStamp);
                    this.TotalMaxTimestamp = Math.Max(this.TotalMaxTimestamp, tag.TimeStamp);
                }
                else
                { tag.SetTimeStamp(0); }
            }
            void ProcessRecordLogic()
            {
                if (this.CuttingMode != AutoCuttingMode.Disabled && tag.IsVideoKeyframe)
                {
                    bool byTime = (this.CuttingMode == AutoCuttingMode.ByTime) && (this.CurrentMaxTimestamp / 1000 >= this.CuttingNumber * 60);
                    bool bySize = (this.CuttingMode == AutoCuttingMode.BySize) && ((this._targetFile.Length / 1024 / 1024) >= this.CuttingNumber);
                    if (byTime || bySize)
                    {
                        this.FinallizeCurrentFile();
                        this.OpenNewRecordFile();
                        this._writeTimeStamp = this.TotalMaxTimestamp;
                    }
                }

                if (!(this._targetFile?.CanWrite ?? false))
                {
                    this.OpenNewRecordFile();
                }

                tag.WriteTo(this._targetFile, this._writeTimeStamp);
            }
            void ProcessClipLogic()
            {
                this._tags.Add(tag);

                // 移除过旧的数据
                if (this.TotalMaxTimestamp - this._lasttimeRemovedTimestamp > 800)
                {
                    this._lasttimeRemovedTimestamp = this.TotalMaxTimestamp;
                    int max_remove_index = this._tags.FindLastIndex(x => x.IsVideoKeyframe && ((this.TotalMaxTimestamp - x.TimeStamp) > (this.ClipLengthPast * SEC_TO_MS)));
                    if (max_remove_index > 0)
                    {
                        this._tags.RemoveRange(0, max_remove_index);
                    }
                    // Tags.RemoveRange(0, max_remove_index + 1 - 1);
                    // 给将来的备注：这里是故意 + 1 - 1 的，因为要保留选中的那个关键帧， + 1 就把关键帧删除了
                }

                this.Clips.ToList().ForEach(fcp => fcp.AddTag(tag));
            }
            void ParseTimestampOffset()
            {
                if (tag.TagType == TagType.VIDEO)
                {
                    this._tagVideoCount++;
                    if (this._tagVideoCount < 2)
                    {
                        logger.Debug("第一个 Video Tag 时间戳 {0} ms", tag.TimeStamp);
                        this._headerTags.Add(tag);
                    }
                    else
                    {
                        this._baseTimeStamp = tag.TimeStamp;
                        this._hasOffset = true;
                        logger.Debug("重设时间戳 {0} 毫秒", this._baseTimeStamp);
                    }
                }
                else if (tag.TagType == TagType.AUDIO)
                {
                    this._tagAudioCount++;
                    if (this._tagAudioCount < 2)
                    {
                        logger.Debug("第一个 Audio Tag 时间戳 {0} ms", tag.TimeStamp);
                        this._headerTags.Add(tag);
                    }
                    else
                    {
                        this._baseTimeStamp = tag.TimeStamp;
                        this._hasOffset = true;
                        logger.Debug("重设时间戳 {0} 毫秒", this._baseTimeStamp);
                    }
                }
            }
            void ParseMetadata()
            {
                if (tag.TagType == TagType.DATA)
                {
                    this._targetFile?.Write(FLV_HEADER_BYTES, 0, FLV_HEADER_BYTES.Length);
                    this._targetFile?.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                    this.Metadata = this.funcFlvMetadata(tag.Data);

                    OnMetaData?.Invoke(this, new FlvMetadataArgs() { Metadata = Metadata });

                    tag.Data = this.Metadata.ToBytes();
                    tag.WriteTo(this._targetFile);
                }
                else
                {
                    throw new Exception("onMetaData not found");
                }
            }
        }

        public IFlvClipProcessor Clip()
        {
            if (!this.EnabledFeature.IsClipEnabled()) { return null; }
            lock (this._writelock)
            {
                if (this._finalized)
                {
                    return null;
                    // throw new InvalidOperationException("Processor Already Closed"); 
                }

                logger.Info("剪辑处理中，将会保存过去 {0} 秒和将来 {1} 秒的直播流", (this._tags[this._tags.Count - 1].TimeStamp - this._tags[0].TimeStamp) / 1000d, this.ClipLengthFuture);
                IFlvClipProcessor clip = this.funcFlvClipProcessor().Initialize(this.GetClipFileName(), this.Metadata, this._headerTags, new List<IFlvTag>(this._tags.ToArray()), this.ClipLengthFuture);
                clip.ClipFinalized += (sender, e) => { this.Clips.Remove(e.ClipProcessor); };
                this.Clips.Add(clip);
                return clip;
            }
        }

        private void FinallizeCurrentFile()
        {
            try
            {
                var fileSize = this._targetFile?.Length ?? -1;
                logger.Debug("正在关闭当前录制文件: " + this._targetFile?.Name);
                this.Metadata["duration"] = this.CurrentMaxTimestamp / 1000.0;
                this.Metadata["lasttimestamp"] = (double)this.CurrentMaxTimestamp;
                byte[] metadata = this.Metadata.ToBytes();

                // 13 for FLV header & "0th" tag size
                // 11 for 1st tag header
                this._targetFile?.Seek(13 + 11, SeekOrigin.Begin);
                this._targetFile?.Write(metadata, 0, metadata.Length);

                if (fileSize > 0)
                    FileFinalized?.Invoke(this, fileSize);
            }
            catch (IOException ex)
            {
                logger.Warn(ex, "保存录制文件时出错");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存录制文件时出错");
            }
            finally
            {
                this._targetFile?.Close();
                this._targetFile = null;
            }
        }

        public void FinallizeFile()
        {
            if (!this._finalized)
            {
                lock (this._writelock)
                {
                    try
                    {
                        this.FinallizeCurrentFile();
                    }
                    finally
                    {
                        this._targetFile?.Close();
                        this._data.Close();
                        this._tags.Clear();

                        this._finalized = true;

                        this.Clips.ToList().ForEach(fcp => fcp.FinallizeFile()); // TODO: check
                        StreamFinalized?.Invoke(this, new StreamFinalizedArgs() { StreamProcessor = this });
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this._data.Dispose();
                    this._targetFile?.Dispose();
                    OnMetaData = null;
                    StreamFinalized = null;
                    TagProcessed = null;
                }
                this._tags.Clear();
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
