using BililiveRecorder.Core.Config;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace BililiveRecorder.Core
{
    public class BasicDanmakuWriter : IBasicDanmakuWriter
    {
        private static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            CloseOutput = true,
            WriteEndDocumentOnClose = true
        };

        private XmlWriter xmlWriter = null;
        private DateTimeOffset offset = DateTimeOffset.UtcNow;
        private readonly ConfigV1 config;

        public BasicDanmakuWriter(ConfigV1 config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public void EnableWithPath(string path)
        {
            if (disposedValue) return;

            semaphoreSlim.Wait();
            try
            {
                if (xmlWriter != null)
                {
                    xmlWriter.Close();
                    xmlWriter.Dispose();
                    xmlWriter = null;
                }

                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch (Exception) { }
                var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
                WriteStartDocument(xmlWriter);
                offset = DateTimeOffset.UtcNow;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void Disable()
        {
            if (disposedValue) return;

            semaphoreSlim.Wait();
            try
            {
                if (xmlWriter != null)
                {
                    xmlWriter.Close();
                    xmlWriter.Dispose();
                    xmlWriter = null;
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public void Write(DanmakuModel danmakuModel)
        {
            if (disposedValue) return;

            semaphoreSlim.Wait();
            try
            {
                if (xmlWriter != null)
                {
                    switch (danmakuModel.MsgType)
                    {
                        case MsgTypeEnum.Comment:
                            {
                                var type = danmakuModel.RawObj?["info"]?[0]?[1]?.ToObject<int>() ?? 1;
                                var size = danmakuModel.RawObj?["info"]?[0]?[2]?.ToObject<int>() ?? 25;
                                var color = danmakuModel.RawObj?["info"]?[0]?[3]?.ToObject<int>() ?? 0XFFFFFF;
                                var st = danmakuModel.RawObj?["info"]?[0]?[4]?.ToObject<long>() ?? 0L;
                                var ts = Math.Max((DateTimeOffset.FromUnixTimeMilliseconds(st) - offset).TotalSeconds, 0d);

                                xmlWriter.WriteStartElement("d");
                                xmlWriter.WriteAttributeString("p", $"{ts},{type},{size},{color},{st},0,{danmakuModel.UserID},0");
                                xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                if (config.RecordDanmakuRaw)
                                    xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["info"]?.ToString(Newtonsoft.Json.Formatting.None));
                                xmlWriter.WriteValue(danmakuModel.CommentText);
                                xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.SuperChat:
                            if (config.RecordDanmakuSuperChat)
                            {
                                xmlWriter.WriteStartElement("sc");
                                var ts = Math.Max((DateTimeOffset.UtcNow - offset).TotalSeconds, 0d);
                                xmlWriter.WriteAttributeString("ts", ts.ToString());
                                xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                xmlWriter.WriteAttributeString("price", danmakuModel.Price.ToString());
                                xmlWriter.WriteAttributeString("time", danmakuModel.SCKeepTime.ToString());
                                if (config.RecordDanmakuRaw)
                                    xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                xmlWriter.WriteValue(danmakuModel.CommentText);
                                xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.GiftSend:
                            if (config.RecordDanmakuGift)
                            {
                                xmlWriter.WriteStartElement("gift");
                                var ts = Math.Max((DateTimeOffset.UtcNow - offset).TotalSeconds, 0d);
                                xmlWriter.WriteAttributeString("ts", ts.ToString());
                                xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                xmlWriter.WriteAttributeString("giftname", danmakuModel.GiftName);
                                xmlWriter.WriteAttributeString("giftcount", danmakuModel.GiftCount.ToString());
                                if (config.RecordDanmakuRaw)
                                    xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.GuardBuy:
                            if (config.RecordDanmakuGuard)
                            {
                                xmlWriter.WriteStartElement("guard");
                                var ts = Math.Max((DateTimeOffset.UtcNow - offset).TotalSeconds, 0d);
                                xmlWriter.WriteAttributeString("ts", ts.ToString());
                                xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                xmlWriter.WriteAttributeString("level", danmakuModel.UserGuardLevel.ToString()); ;
                                xmlWriter.WriteAttributeString("count", danmakuModel.GiftCount.ToString());
                                if (config.RecordDanmakuRaw)
                                    xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                xmlWriter.WriteEndElement();
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private void WriteStartDocument(XmlWriter writer)
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("i");
            writer.WriteAttributeString("BililiveRecorder", "B站录播姬拓展版弹幕文件");
            writer.WriteComment("\nB站录播姬 " + BuildInfo.Version + "  " + BuildInfo.HeadSha1 + "\n本文件在B站主站视频弹幕XML格式的基础上进行了拓展\n\nsc为SuperChat\ngift为礼物\nguard为上船\n\nattribute \"raw\" 为原始数据\n");
            writer.WriteElementString("chatserver", "chat.bilibili.com");
            writer.WriteElementString("chatid", "0");
            writer.WriteElementString("mission", "0");
            writer.WriteElementString("maxlimit", "1000");
            writer.WriteElementString("state", "0");
            writer.WriteElementString("real_name", "0");
            writer.WriteElementString("source", "0");
            writer.Flush();
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    semaphoreSlim.Dispose();
                    xmlWriter?.Close();
                    xmlWriter?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
