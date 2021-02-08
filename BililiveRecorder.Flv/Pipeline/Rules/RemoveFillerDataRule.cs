using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 删除 H.264 Filler Data
    /// </summary>
    /// <remarks>
    /// 部分直播码率瞎填的主播的直播数据中存在大量无用的 Filler Data<br/>
    /// 录制这些主播时删除这些数据可以节省硬盘空间<br/>
    /// <br/>
    /// 本规则应该放在一般规则前面
    /// </remarks>
    public class RemoveFillerDataRule : ISimpleProcessingRule
    {
        public async Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            // 先运行下层规则
            await next().ConfigureAwait(false);

            // 处理下层规则返回的数据
            context.Output.ForEach(action =>
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
