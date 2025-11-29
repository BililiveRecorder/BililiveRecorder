using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BililiveRecorder.ToolBox.Tool.DanmakuMerger
{
    public class DanmakuMergerHandler : ICommandHandler<DanmakuMergerRequest, DanmakuMergerResponse>
    {
        private static readonly string[] DanmakuElementNames = new[] { "d", "gift", "sc", "guard" };

        public string Name => "Merge Danmaku";

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<CommandResponse<DanmakuMergerResponse>> Handle(DanmakuMergerRequest request, CancellationToken cancellationToken, ProgressCallback? progress)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var inputLength = request.Inputs.Length;

            if (inputLength < 2)
                return new CommandResponse<DanmakuMergerResponse>
                {
                    Status = ResponseStatus.Error,
                    ErrorMessage = "At least 2 input files required"
                };

            if (request.Offsets is not null)
            {
                if (request.Offsets.Length != inputLength)
                {
                    return new CommandResponse<DanmakuMergerResponse>
                    {
                        Status = ResponseStatus.Error,
                        ErrorMessage = "The number of offsets should match the number of input files."
                    };
                }
            }

            var files = new FileStream[inputLength];
            var readers = new XmlReader?[inputLength];

            FileStream? outputFile = null;
            XmlWriter? writer = null;
            XElement recordInfo;

            TimeSpan[] timeDiff;

            try // finally

            {
                // 读取文件开头并计算时间差
                try
                {
                    DateTimeOffset baseTime;

                    // 打开输入文件
                    for (var i = 0; i < inputLength; i++)
                    {
                        var file = File.Open(request.Inputs[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                        files[i] = file;
                        readers[i] = XmlReader.Create(file, null);
                    }

                    // 读取XML文件开头
                    var startTimes = new (DateTimeOffset time, XElement element)[inputLength];
                    for (var i = 0; i < inputLength; i++)
                    {
                        var r = readers[i]!;
                        r.ReadStartElement("i");
                        while (r.Name != "i")
                        {
                            if (r.Name == "BililiveRecorderRecordInfo")
                            {
                                var el = (XNode.ReadFrom(r) as XElement)!;
                                var time = (DateTimeOffset)el.Attribute("start_time");
                                startTimes[i] = (time, el);
                                break;
                            }
                            else
                            {
                                r.Skip();
                            }
                        }
                    }

                    if (request.Offsets is not null)
                    {
                        // 使用传递进来的参数作为时间差
                        timeDiff = request.Offsets.Select(x => TimeSpan.FromSeconds(x)).ToArray();
                        var (time, element) = startTimes[Array.IndexOf(timeDiff, timeDiff.Min())];
                        recordInfo = element;
                        baseTime = time;
                    }
                    else
                    {
                        // 使用文件内的开始时间作为时间差
                        var (time, element) = startTimes.OrderBy(x => x.time).First();
                        recordInfo = element;
                        baseTime = time;
                        timeDiff = startTimes.Select(x => x.time - baseTime).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    return new CommandResponse<DanmakuMergerResponse>
                    {
                        Status = ResponseStatus.InputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                try
                {
                    // 打开输出文件
                    outputFile = File.Open(request.Output, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                    writer = XmlWriter.Create(outputFile, new XmlWriterSettings
                    {
                        Indent = true,
                        IndentChars = "  ",
                        Encoding = Encoding.UTF8,
                        CloseOutput = true,
                        WriteEndDocumentOnClose = true,
                    });

                    // 写入文件开头
                    writer.WriteStartDocument();
                    writer.WriteProcessingInstruction("xml-stylesheet", "type=\"text/xsl\" href=\"#s\"");
                    writer.WriteStartElement("i");
                    writer.WriteComment("\nmikufans录播姬 " + GitVersionInformation.InformationalVersion + " 使用工具箱合并\nhttps://rec.danmuji.org/user/danmaku/\n本文件的弹幕信息兼容mikufans主站视频弹幕XML格式\n本XML自带样式可以在浏览器里打开（推荐使用Chrome）\n\nsc 为SuperChat\ngift为礼物\nguard为上船\n\nattribute \"raw\" 为原始数据\n");
                    writer.WriteElementString("chatserver", "chat.bilibili.com");
                    writer.WriteElementString("chatid", "0");
                    writer.WriteElementString("mission", "0");
                    writer.WriteElementString("maxlimit", "1000");
                    writer.WriteElementString("state", "0");
                    writer.WriteElementString("real_name", "0");
                    writer.WriteElementString("source", "0");

                    writer.WriteStartElement("BililiveRecorder");
                    writer.WriteAttributeString("version", GitVersionInformation.FullSemVer);
                    writer.WriteAttributeString("merged", "by toolbox");
                    writer.WriteEndElement();

                    // 写入直播间信息
                    recordInfo.WriteTo(writer);

                    // see BililiveRecorder.Core\Danmaku\BasicDanmakuWriter.cs
                    const string style = @"<z:stylesheet version=""1.0"" id=""s"" xml:id=""s"" xmlns:z=""http://www.w3.org/1999/XSL/Transform""><z:output method=""html""/><z:template match=""/""><html><meta name=""viewport"" content=""width=device-width""/><title>mikufans录播姬弹幕文件 - <z:value-of select=""/i/BililiveRecorderRecordInfo/@name""/></title><style>body{margin:0}h1,h2,p,table{margin-left:5px}table{border-spacing:0}td,th{border:1px solid grey;padding:1px}th{position:sticky;top:0;background:#4098de}tr:hover{background:#d9f4ff}div{overflow:auto;max-height:80vh;max-width:100vw;width:fit-content}</style><h1><a href=""https://rec.danmuji.org"">mikufans录播姬</a>弹幕XML文件</h1><p>本文件不支持在 IE 浏览器里预览，请使用 Chrome Firefox Edge 等浏览器。</p><p>文件用法参考文档 <a href=""https://rec.danmuji.org/user/danmaku/"">https://rec.danmuji.org/user/danmaku/</a></p><table><tr><td>录播姬版本</td><td><z:value-of select=""/i/BililiveRecorder/@version""/></td></tr><tr><td>房间号</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@roomid""/></td></tr><tr><td>主播名</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@name""/></td></tr><tr><td>录制开始时间</td><td><z:value-of select=""/i/BililiveRecorderRecordInfo/@start_time""/></td></tr><tr><td><a href=""#d"">弹幕</a></td><td>共<z:value-of select=""count(/i/d)""/>条记录</td></tr><tr><td><a href=""#guard"">上船</a></td><td>共<z:value-of select=""count(/i/guard)""/>条记录</td></tr><tr><td><a href=""#sc"">SC</a></td><td>共<z:value-of select=""count(/i/sc)""/>条记录</td></tr><tr><td><a href=""#gift"">礼物</a></td><td>共<z:value-of select=""count(/i/gift)""/>条记录</td></tr></table><h2 id=""d"">弹幕</h2><div id=""dm""><table><tr><th>用户名</th><th>出现时间</th><th>用户ID</th><th>弹幕</th><th>参数</th></tr><z:for-each select=""/i/d""><tr><td><z:value-of select=""@user""/></td><td></td><td></td><td><z:value-of select="".""/></td><td><z:value-of select=""@p""/></td></tr></z:for-each></table></div><script>Array.from(document.querySelectorAll('#dm tr')).slice(1).map(t=>t.querySelectorAll('td')).forEach(t=>{let p=t[4].textContent.split(','),a=p[0];t[1].textContent=`${(Math.floor(a/60/60)+'').padStart(2,0)}:${(Math.floor(a/60%60)+'').padStart(2,0)}:${(a%60).toFixed(3).padStart(6,0)}`;t[2].innerHTML=`&lt;a target=_blank rel=""nofollow noreferrer"" href=""https://space.bilibili.com/${p[6]}""&gt;${p[6]}&lt;/a&gt;`})</script><h2 id=""guard"">舰长购买</h2><div><table><tr><th>用户名</th><th>用户ID</th><th>舰长等级</th><th>购买数量</th><th>出现时间</th></tr><z:for-each select=""/i/guard""><tr><td><z:value-of select=""@user""/></td><td><a rel=""nofollow noreferrer""><z:attribute name=""href""><z:text>https://space.bilibili.com/</z:text><z:value-of select=""@uid"" /></z:attribute><z:value-of select=""@uid""/></a></td><td><z:value-of select=""@level""/></td><td><z:value-of select=""@count""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div><h2 id=""sc"">SuperChat 醒目留言</h2><div><table><tr><th>用户名</th><th>用户ID</th><th>内容</th><th>显示时长</th><th>价格</th><th>出现时间</th></tr><z:for-each select=""/i/sc""><tr><td><z:value-of select=""@user""/></td><td><a rel=""nofollow noreferrer""><z:attribute name=""href""><z:text>https://space.bilibili.com/</z:text><z:value-of select=""@uid"" /></z:attribute><z:value-of select=""@uid""/></a></td><td><z:value-of select="".""/></td><td><z:value-of select=""@time""/></td><td><z:value-of select=""@price""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div><h2 id=""gift"">礼物</h2><div><table><tr><th>用户名</th><th>用户ID</th><th>礼物名</th><th>礼物数量</th><th>出现时间</th></tr><z:for-each select=""/i/gift""><tr><td><z:value-of select=""@user""/></td><td><a rel=""nofollow noreferrer""><z:attribute name=""href""><z:text>https://space.bilibili.com/</z:text><z:value-of select=""@uid"" /></z:attribute><z:value-of select=""@uid""/></a></td><td><z:value-of select=""@giftname""/></td><td><z:value-of select=""@giftcount""/></td><td><z:value-of select=""@ts""/></td></tr></z:for-each></table></div></html></z:template></z:stylesheet>";

                    writer.WriteStartElement("BililiveRecorderXmlStyle");
                    writer.WriteRaw(style);
                    writer.WriteEndElement();
                }
                catch (Exception ex)
                {
                    return new CommandResponse<DanmakuMergerResponse>
                    {
                        Status = ResponseStatus.OutputIOError,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                try
                {
                    var els = new List<(TimeSpan time, XElement el, int reader)>();

                    // 取出所有文件里第一条数据
                    for (var i = 0; i < inputLength; i++)
                    {
                        var r = readers[i]!;
                        XElement? el = null;
                        
                        try
                        {
                            el = ReadDanmakuElement(r);
                        }
                        catch (Exception readEx)
                        {
                            return new CommandResponse<DanmakuMergerResponse>
                            {
                                Status = ResponseStatus.InputIOError,
                                Exception = readEx,
                                ErrorMessage = request.Inputs[i] + ": " + readEx.Message
                            };
                        }

                        if (el is null)
                        {
                            readers[i] = null;
                            continue;
                        }
                        var time = UpdateTimestamp(el, timeDiff[i]);
                        els.Add((time, el, i));
                    }

                    // 排序
                    els.Sort((a, b) => a.time.CompareTo(b.time));

                    while (true)
                    {
                        // 写入时间最小的数据

                        // 所有数据写完就退出循环
                        if (els.Count == 0)
                            break;

                        (_, var el, var readerIndex) = els[0];
                        el.WriteTo(writer);
                        els.RemoveAt(0);

                        // 读取一个新的数据

                        var reader = readers[readerIndex];
                        // 检查这个文件是否还有更多数据
                        if (reader is not null)
                        {
                        readNextElementFromSameReader:
                        
                            XElement? newEl = null;

                            try
                            {
                                newEl = ReadDanmakuElement(reader);
                            }
                            catch (Exception readEx)
                            {
                                return new CommandResponse<DanmakuMergerResponse>
                                {
                                    Status = ResponseStatus.InputIOError,
                                    Exception = readEx,
                                    ErrorMessage = request.Inputs[readerIndex] + ": " + readEx.Message
                                };
                            }

                            if (newEl is null)
                            {
                                // 文件已结束
                                reader.Dispose();
                                readers[readerIndex] = null;
                                continue;
                            }
                            else
                            {
                                // 计算新的时间
                                var newTime = UpdateTimestamp(newEl, timeDiff[readerIndex]);
                                if (els.Count < 1 || newTime < els[0].time)
                                {
                                    // 如果这是最后一个文件，或当前数据的时间是所有数据里最小的
                                    // 直接写入输出文件
                                    newEl.WriteTo(writer);
                                    goto readNextElementFromSameReader;
                                }
                                else
                                {
                                    // 如果其他数据比本数据的时间更小
                                    // 添加到列表中，后面排序再来
                                    els.Add((newTime, newEl, readerIndex));
                                    els.Sort((a, b) => a.time.CompareTo(b.time));
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new CommandResponse<DanmakuMergerResponse>
                    {
                        Status = ResponseStatus.Error,
                        Exception = ex,
                        ErrorMessage = ex.Message
                    };
                }

                return new CommandResponse<DanmakuMergerResponse> { Status = ResponseStatus.OK, Data = new DanmakuMergerResponse() };
            }
            finally
            {
                try
                {
                    writer?.Dispose();
                    outputFile?.Dispose();
                }
                catch (Exception) { }

                for (var i = 0; i < inputLength; i++)
                {
                    try
                    {
                        readers[i]?.Dispose();
                        files[i]?.Dispose();
                    }
                    catch (Exception) { }
                }
            }
        }

        private static XElement? ReadDanmakuElement(XmlReader r)
        {
            while (r.Name != "i")
            {
                if (DanmakuElementNames.Contains(r.Name))
                {
                    var el = (XNode.ReadFrom(r) as XElement)!;
                    return el;
                }
                else
                {
                    r.Skip();
                }
            }
            return null;
        }

        private static TimeSpan UpdateTimestamp(XElement element, TimeSpan offset)
        {
            switch (element.Name.LocalName)
            {
                case "d":
                    {
                        var p = element.Attribute("p");
                        var i = p.Value.IndexOf(',');
                        var t = TimeSpan.FromSeconds(double.Parse(p.Value.Substring(0, i)));
                        t += offset;
                        p.Value = t.TotalSeconds.ToString("F3") + p.Value.Substring(i);
                        return t;
                    }
                case "gift":
                case "sc":
                case "guard":
                    {
                        var ts = TimeSpan.FromSeconds((double)element.Attribute("ts"));
                        ts += offset;
                        element.SetAttributeValue("ts", ts.TotalSeconds.ToString("F3"));
                        return ts;
                    }
                default:
                    return default;
            }
        }
    }
}
