using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace BililiveRecorder.FlvProcessor
{
    public class FlvClipProcessor : IFlvClipProcessor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public IFlvMetadata Header { get; private set; }
        public List<IFlvTag> HTags { get; private set; }
        public List<IFlvTag> Tags { get; private set; }
        private int target = -1;

        public Func<string> GetFileName { get; set; }

        public FlvClipProcessor()
        {
        }

        public IFlvClipProcessor Initialize(IFlvMetadata metadata, List<IFlvTag> head, List<IFlvTag> data, uint seconds)
        {
            Header = metadata;
            HTags = head;
            Tags = data;
            target = Tags[Tags.Count - 1].TimeStamp + (int)(seconds * FlvStreamProcessor.SEC_TO_MS);
            logger.Debug("Clip 创建 Tags.Count={0} Tags[0].TimeStamp={1} Tags[Tags.Count-1].TimeStamp={2} Tags里秒数={3}",
                Tags.Count, Tags[0].TimeStamp, Tags[Tags.Count - 1].TimeStamp, (Tags[Tags.Count - 1].TimeStamp - Tags[0].TimeStamp) / 1000d);

            return this;
        }

        public void AddTag(IFlvTag tag)
        {
            Tags.Add(tag);
            if (tag.TimeStamp >= target)
            {
                FinallizeFile();
            }
        }

        public void FinallizeFile()
        {
            try
            {
                string filepath = GetFileName();
                using (var fs = new FileStream(filepath, FileMode.CreateNew, FileAccess.ReadWrite))
                {
                    fs.Write(FlvStreamProcessor.FLV_HEADER_BYTES, 0, FlvStreamProcessor.FLV_HEADER_BYTES.Length);
                    fs.Write(new byte[] { 0, 0, 0, 0, }, 0, 4);

                    Header.Meta["duration"] = (Tags[Tags.Count - 1].TimeStamp - Tags[0].TimeStamp) / 1000d;
                    Header.Meta["lasttimestamp"] = (Tags[Tags.Count - 1].TimeStamp - Tags[0].TimeStamp);

                    var t = new FlvTag
                    {
                        TagType = TagType.DATA,
                        Data = Header.ToBytes()
                    };
                    t.WriteTo(fs);

                    int offset = Tags[0].TimeStamp;

                    HTags.ForEach(tag => tag.WriteTo(fs));
                    Tags.ForEach(tag => tag.WriteTo(fs, offset));

                    logger.Info("剪辑已保存：{0}", Path.GetFileName(filepath));

                    fs.Close();
                }
                Tags.Clear();

                ClipFinalized?.Invoke(this, new ClipFinalizedArgs() { ClipProcessor = this });

            }
            catch (Exception ex)
            {
                logger.Error(ex, "保存剪辑文件时出错");
            }
        }

        public event ClipFinalizedEvent ClipFinalized;
    }
}
