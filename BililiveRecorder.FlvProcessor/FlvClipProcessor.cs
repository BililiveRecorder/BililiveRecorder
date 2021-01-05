using System;
using System.Collections.Generic;
using System.IO;
using NLog;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor : IFlvClipProcessor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Func<IFlvTag> funcFlvTag;

        public IFlvMetadata Header { get; private set; }
        public List<IFlvTag> HTags { get; private set; }
        public List<IFlvTag> Tags { get; private set; }
        private int target = -1;
        private string path;

        public FlvClipProcessor(Func<IFlvTag> funcFlvTag)
        {
            this.funcFlvTag = funcFlvTag;
        }

        public IFlvClipProcessor Initialize(string path, IFlvMetadata metadata, List<IFlvTag> head, List<IFlvTag> data, uint seconds)
        {
            this.path = path;
            this.Header = metadata; // TODO: Copy a copy, do not share
            this.HTags = head;
            this.Tags = data;
            this.target = this.Tags[this.Tags.Count - 1].TimeStamp + (int)(seconds * FlvStreamProcessor.SEC_TO_MS);
            logger.Debug("Clip 创建 Tags.Count={0} Tags[0].TimeStamp={1} Tags[Tags.Count-1].TimeStamp={2} Tags里秒数={3}",
                this.Tags.Count, this.Tags[0].TimeStamp, this.Tags[this.Tags.Count - 1].TimeStamp, (this.Tags[this.Tags.Count - 1].TimeStamp - this.Tags[0].TimeStamp) / 1000d);

            return this;
        }

        public void AddTag(IFlvTag tag)
        {
            this.Tags.Add(tag);
            if (tag.TimeStamp >= this.target)
            {
                this.FinallizeFile();
            }
        }

        public void FinallizeFile()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(this.path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(this.path));
                }
                using (var fs = new FileStream(this.path, FileMode.CreateNew, FileAccess.ReadWrite))
                {
                    fs.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                    fs.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                    double clipDuration = (this.Tags[this.Tags.Count - 1].TimeStamp - this.Tags[0].TimeStamp) / 1000d;
                    this.Header["duration"] = clipDuration;
                    this.Header["lasttimestamp"] = (double)(this.Tags[this.Tags.Count - 1].TimeStamp - this.Tags[0].TimeStamp);

                    var t = this.funcFlvTag();
                    t.TagType = TagType.DATA;

                    if (this.Header.ContainsKey("BililiveRecorder"))
                    {
                        // TODO: 更好的写法
                        (this.Header["BililiveRecorder"] as Dictionary<string, object>)["starttime"] = DateTime.UtcNow - TimeSpan.FromSeconds(clipDuration);
                    }
                    t.Data = this.Header.ToBytes();
                    t.WriteTo(fs);

                    int offset = this.Tags[0].TimeStamp;

                    this.HTags.ForEach(tag => tag.WriteTo(fs));
                    this.Tags.ForEach(tag => tag.WriteTo(fs, offset));

                    logger.Info("剪辑已保存：{0}", Path.GetFileName(this.path));

                    fs.Close();
                }
                this.Tags.Clear();
            }
            catch (IOException ex)
            {
                logger.Warn(ex, "保存剪辑文件时出错");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存剪辑文件时出错");
            }
            ClipFinalized?.Invoke(this, new ClipFinalizedArgs() { ClipProcessor = this });
        }

        public event ClipFinalizedEvent ClipFinalized;
    }
}
