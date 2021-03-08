using System;
using System.Collections.Generic;
using BililiveRecorder.Flv.Amf;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 处理收到 Script Tag 的情况
    /// </summary>
    /// <remarks>
    /// 本规则为一般规则
    /// </remarks>
    public class HandleNewScriptRule : ISimpleProcessingRule
    {
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.Other, "收到了非 onMetaData 的 Script Tag");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineScriptAction scriptAction)
            {
                var data = scriptAction.Tag.ScriptData;
                if (!(data is null)
                    && data.Values.Count == 2
                    && data.Values[0] is ScriptDataString name
                    && name == "onMetaData")
                {
                    ScriptDataEcmaArray? value = data.Values[1] switch
                    {
                        ScriptDataObject obj => obj,
                        ScriptDataEcmaArray arr => arr,
                        _ => null
                    };

                    if (value is null)
                        value = new ScriptDataEcmaArray();

                    yield return PipelineNewFileAction.Instance;
                    yield return (new PipelineScriptAction(new Tag
                    {
                        Type = TagType.Script,
                        ScriptData = new ScriptTagBody(new List<IScriptDataValue>
                        {
                            name,
                            value
                        })
                    }));
                }
                else
                {
                    context.AddComment(comment);
                }
            }
            else
                yield return action;
        }
    }
}
