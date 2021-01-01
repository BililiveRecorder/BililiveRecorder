using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using BililiveRecorder.Core.Config.V2;

#nullable enable
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

        private static readonly Regex invalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);
        private static string RemoveInvalidXMLChars(string text) => string.IsNullOrEmpty(text) ? string.Empty : invalidXMLChars.Replace(text, string.Empty);

        private XmlWriter? xmlWriter = null;
        private DateTimeOffset offset = DateTimeOffset.UtcNow;
        private uint writeCount = 0;
        private readonly RoomConfig config;

        public BasicDanmakuWriter(RoomConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public void EnableWithPath(string path, IRecordedRoom recordedRoom)
        {
            if (this.disposedValue) return;

            this.semaphoreSlim.Wait();
            try
            {
                if (this.xmlWriter != null)
                {
                    this.xmlWriter.Close();
                    this.xmlWriter.Dispose();
                    this.xmlWriter = null;
                }

                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); } catch (Exception) { }
                var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);

                this.xmlWriter = XmlWriter.Create(stream, xmlWriterSettings);
                this.WriteStartDocument(this.xmlWriter, recordedRoom);
                this.offset = DateTimeOffset.UtcNow;
                this.writeCount = 0;
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        public void Disable()
        {
            if (this.disposedValue) return;

            this.semaphoreSlim.Wait();
            try
            {
                if (this.xmlWriter != null)
                {
                    this.xmlWriter.Close();
                    this.xmlWriter.Dispose();
                    this.xmlWriter = null;
                }
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        public void Write(DanmakuModel danmakuModel)
        {
            if (this.disposedValue) return;

            this.semaphoreSlim.Wait();
            try
            {
                if (this.xmlWriter != null)
                {
                    var write = true;
                    var recordDanmakuRaw = this.config.RecordDanmakuRaw;
                    switch (danmakuModel.MsgType)
                    {
                        case MsgTypeEnum.Comment:
                            {
                                var type = danmakuModel.RawObj?["info"]?[0]?[1]?.ToObject<int>() ?? 1;
                                var size = danmakuModel.RawObj?["info"]?[0]?[2]?.ToObject<int>() ?? 25;
                                var color = danmakuModel.RawObj?["info"]?[0]?[3]?.ToObject<int>() ?? 0XFFFFFF;
                                var st = danmakuModel.RawObj?["info"]?[0]?[4]?.ToObject<long>() ?? 0L;
                                var ts = Math.Max((DateTimeOffset.FromUnixTimeMilliseconds(st) - this.offset).TotalSeconds, 0d);

                                this.xmlWriter.WriteStartElement("d");
                                this.xmlWriter.WriteAttributeString("p", $"{ts},{type},{size},{color},{st},0,{danmakuModel.UserID},0");
                                this.xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                if (recordDanmakuRaw)
                                    this.xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["info"]?.ToString(Newtonsoft.Json.Formatting.None));
                                this.xmlWriter.WriteValue(RemoveInvalidXMLChars(danmakuModel.CommentText));
                                this.xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.SuperChat:
                            if (this.config.RecordDanmakuSuperChat)
                            {
                                this.xmlWriter.WriteStartElement("sc");
                                var ts = Math.Max((DateTimeOffset.UtcNow - this.offset).TotalSeconds, 0d);
                                this.xmlWriter.WriteAttributeString("ts", ts.ToString());
                                this.xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                this.xmlWriter.WriteAttributeString("price", danmakuModel.Price.ToString());
                                this.xmlWriter.WriteAttributeString("time", danmakuModel.SCKeepTime.ToString());
                                if (recordDanmakuRaw)
                                    this.xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                this.xmlWriter.WriteValue(RemoveInvalidXMLChars(danmakuModel.CommentText));
                                this.xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.GiftSend:
                            if (this.config.RecordDanmakuGift)
                            {
                                this.xmlWriter.WriteStartElement("gift");
                                var ts = Math.Max((DateTimeOffset.UtcNow - this.offset).TotalSeconds, 0d);
                                this.xmlWriter.WriteAttributeString("ts", ts.ToString());
                                this.xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                this.xmlWriter.WriteAttributeString("giftname", danmakuModel.GiftName);
                                this.xmlWriter.WriteAttributeString("giftcount", danmakuModel.GiftCount.ToString());
                                if (recordDanmakuRaw)
                                    this.xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                this.xmlWriter.WriteEndElement();
                            }
                            break;
                        case MsgTypeEnum.GuardBuy:
                            if (this.config.RecordDanmakuGuard)
                            {
                                this.xmlWriter.WriteStartElement("guard");
                                var ts = Math.Max((DateTimeOffset.UtcNow - this.offset).TotalSeconds, 0d);
                                this.xmlWriter.WriteAttributeString("ts", ts.ToString());
                                this.xmlWriter.WriteAttributeString("user", danmakuModel.UserName);
                                this.xmlWriter.WriteAttributeString("level", danmakuModel.UserGuardLevel.ToString()); ;
                                this.xmlWriter.WriteAttributeString("count", danmakuModel.GiftCount.ToString());
                                if (recordDanmakuRaw)
                                    this.xmlWriter.WriteAttributeString("raw", danmakuModel.RawObj?["data"]?.ToString(Newtonsoft.Json.Formatting.None));
                                this.xmlWriter.WriteEndElement();
                            }
                            break;
                        default:
                            write = false;
                            break;
                    }

                    if (write && this.writeCount++ >= this.config.RecordDanmakuFlushInterval)
                    {
                        this.xmlWriter.Flush();
                        this.writeCount = 0;
                    }
                }
            }
            finally
            {
                this.semaphoreSlim.Release();
            }
        }

        private void WriteStartDocument(XmlWriter writer, IRecordedRoom recordedRoom)
        {
            writer.WriteStartDocument();
            writer.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"#s\"");
            writer.WriteStartElement("i");
            writer.WriteAttributeString("BililiveRecorder", "B站录播姬弹幕文件");
            writer.WriteComment("\nB站录播姬 " + BuildInfo.Version + " " + BuildInfo.HeadSha1 + "\n本文件的弹幕信息兼容B站主站视频弹幕XML格式\n本XML自带样式可以在浏览器里打开（推荐使用Chrome）\n\nsc 为SuperChat\ngift为礼物\nguard为上船\n\nattribute \"raw\" 为原始数据\n");
            writer.WriteElementString("chatserver", "chat.bilibili.com");
            writer.WriteElementString("chatid", "0");
            writer.WriteElementString("mission", "0");
            writer.WriteElementString("maxlimit", "1000");
            writer.WriteElementString("state", "0");
            writer.WriteElementString("real_name", "0");
            writer.WriteElementString("source", "0");
            writer.WriteStartElement("BililiveRecorder");
            writer.WriteAttributeString("version", BuildInfo.Version + "-" + BuildInfo.HeadShaShort);
            writer.WriteEndElement();
            writer.WriteStartElement("BililiveRecorderRecordInfo");
            writer.WriteAttributeString("roomid", recordedRoom.RoomId.ToString());
            writer.WriteAttributeString("name", recordedRoom.StreamerName);
            writer.WriteAttributeString("start_time", DateTimeOffset.Now.ToString("O"));
            writer.WriteEndElement();
            const string style = @"<z:stylesheet version=""1.0"" id=""s"" xml:id=""s"" xmlns:z=""http://www.w3.org/1999/XSL/Transform""><z:output method=""html""/><z:template match=""/""><html><meta name=""viewport"" content=""width=device-width""/><title>B站录播姬弹幕文件 - <z:value-of select=""/i/BililiveRecorderRecordInfo/@name""/></title><style>body{margin:0}h1,h2,p,table{margin-left:5px}table{border-spacing:0}td,th{border:1px solid grey;padding:1px}th{position:sticky;top:0;background:#4098de}tr:hover{background:#d9f4ff}div{overflow:auto;max-height:80vh;max-width:100vw;width:fit-content}</style><h1>B站录播姬弹幕XML文件</h1><p>本文件的弹幕信息兼容B站主站视频弹幕XML格式，可以使用现有的转换工具把文件中的弹幕转为ass字幕文件</p><table><tr><td>录播姬版本</td><td><z:value-of select=""/i/BililiveRecorder/@version""/></td></tr><tr><td>房间号</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@roomid""/></td></tr><tr><td>主播名</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@name""/></td></tr><tr><td>录制开始时间</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@start_time""/></td></tr><tr><td><a href=""#d"">弹幕</a></td><td>共 <z:value-of select=""count(/i/d)""/> 条记录</td></tr><tr><td><a href=""#guard"">上船</a></td><td>共 <z:value-of select=""count(/i/guard)""/> 条记录</td></tr><tr><td><a href=""#sc"">SC</a></td><td>共 <z:value-of select=""count(/i/sc)""/> 条记录</td></tr><tr><td><a href=""#gift"">礼物</a></td><td>共 <z:value-of select=""count(/i/gift)""/> 条记录</td></tr></table><h2 id=""d"">弹幕</h2><div><table><tr><th>用户名</th><th>弹幕</th><th>参数</th></tr><z:for-each select=""/i/d""><tr><td><z:value-of select=""@user""/></td><td><z:value-of select="".""/></td><td><z:value-of select=""@p""/></td></tr></z:for-each></table></div><h2 id=""guard"">舰长购买</h2><div><table><tr><th>用户名</th><th>舰长等级</th><th>购买数量</th><th>出现时间</th></tr><z:for-each select=""/i/guard""><tr><td><z:value-of select=""@user""/></td><td><z:value-of select=""@level""/></td><td><z:value-of select=""@count""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div><h2 id=""sc"">SuperChat 醒目留言</h2><div><table><tr><th>用户名</th><th>内容</th><th>显示时长</th><th>价格</th><th>出现时间</th></tr><z:for-each select=""/i/sc""><tr><td><z:value-of select=""@user""/></td><td><z:value-of select="".""/></td><td><z:value-of select=""@time""/></td><td><z:value-of select=""@price""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div><h2 id=""gift"">礼物</h2><div><table><tr><th>用户名</th><th>礼物名</th><th>礼物数量</th><th>出现时间</th></tr><z:for-each select=""/i/gift""><tr><td><z:value-of select=""@user""/></td><td><z:value-of select=""@giftname""/></td><td><z:value-of select=""@giftcount""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div></html></z:template></z:stylesheet>";
            writer.WriteRaw("\n\n" + style + "\n\n");
            writer.Flush();
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.semaphoreSlim.Dispose();
                    this.xmlWriter?.Close();
                    this.xmlWriter?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
