using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (context.OriginalInput is PipelineScriptAction scriptAction)
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

                    context.ClearOutput();
                    context.AddNewFileAtStart();
                    context.Output.Add(new PipelineScriptAction(new Tag
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
                    context.AddComment("收到了非 onMetaData 的 Script Tag");
                    context.ClearOutput();
                }
                return Task.CompletedTask;
            }
            else
                return next();
        }
    }
}
