using System;
using System.Collections.Generic;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理 Script Tag
    /// </summary>
    public class HandleNewScriptRule : ISimpleProcessingRule
    {
        private const string STORE_KEY = "HandleNewScriptRule_MetaDataReceived";
        private const string onMetaData = "onMetaData";
        private static readonly ProcessingComment comment_onmetadata = new ProcessingComment(CommentType.OnMetaData, "收到了 onMetaData");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            ScriptTagBody? data;
            if (action is PipelineScriptAction scriptAction)
            {
                data = scriptAction.Tag.ScriptData;
                if (data is not null)
                {
                    if (data.Values.Count == 2
                        && data.Values[0] is ScriptDataString name
                        && name == onMetaData)
                    {
                        goto IsOnMetaData;
                    }
                    else if (data.Values.Count == 3
                        && data.Values[2] is ScriptDataNull
                        && data.Values[0] is ScriptDataString name2
                        && name2 == onMetaData)
                    {
                        /*       d1--ov-gotcha07.bilivideo.com
                         * CNAME d1--ov-gotcha07.bilivideo.com.a.bcelive.com
                         * CNAME d1--ov-gotcha07.bilivideo.com.zengslb.com
                         * Singapore AS21859 Zenlayer Inc 
                         * 
                         * 给的 script tag 数据里第三个位置多了个 NULL
                         */
                        goto IsOnMetaData;
                    }
                    else
                    {
                        goto notOnMetaData;
                    }
                }
                else
                {
                    goto notOnMetaData;
                }
            }
            else
            {
                yield return action;
                yield break;
            }

        IsOnMetaData:
            ScriptDataEcmaArray value = data.Values[1] switch
            {
                ScriptDataObject obj => obj,
                ScriptDataEcmaArray arr => arr,
                _ => new ScriptDataEcmaArray()
            };

            var metaDataReceived = context.SessionItems.ContainsKey(STORE_KEY);
            if (!metaDataReceived)
            {
                context.SessionItems[STORE_KEY] = true;

                context.AddComment(comment_onmetadata);

                yield return PipelineNewFileAction.Instance;
                yield return (new PipelineScriptAction(new Tag
                {
                    Type = TagType.Script,
                    ScriptData = new ScriptTagBody(new List<IScriptDataValue>
                {
                    (ScriptDataString)onMetaData,
                    value
                })
                }));
                yield break;
            }
            else
            {
                // 记录信息，不输出到文件，不对文件进行分段。
                var message = $"重复收到 onMetaData, onMetaData 内容: {data?.ToJson() ?? "(null)"}";
                context.AddComment(new ProcessingComment(CommentType.OnMetaData, message));
                yield return new PipelineLogMessageWithLocationAction(Serilog.Events.LogEventLevel.Warning, "重复收到 onMetaData");
                yield break;
            }
        notOnMetaData:
            context.AddComment(new ProcessingComment(CommentType.Logging, "收到了非 onMetaData 的 Script Tag: " + (data?.ToJson() ?? "(null)")));
            yield break;
        }
    }
}
