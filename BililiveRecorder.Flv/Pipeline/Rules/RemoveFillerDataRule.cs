using System;
using BililiveRecorder.Flv.Pipeline.Actions;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 删除 H.264 Filler Data，节省硬盘空间。
    /// </summary>
    public class RemoveFillerDataRule : ISimpleProcessingRule
    {
        public void Run(FlvProcessingContext context, Action next)
        {
            // 先运行下层规则
            next();

            // 处理下层规则返回的数据
            context.Actions.ForEach(action =>
            {
                if (action is PipelineDataAction dataAction)
                    foreach (var tag in dataAction.Tags)
                        if (tag.Nalus != null)
                            // 虽然这里处理的是 Output 但是因为与 Input 共享同一个 object 所以会把 Input 一起改掉
                            // tag.Nalus = tag.Nalus.Where(x => x.Type != H264NaluType.FillerData).ToList();
                            tag.Nalus.RemoveAll(x => x.Type == H264NaluType.FillerData);
            });

            return;
        }
    }
}
