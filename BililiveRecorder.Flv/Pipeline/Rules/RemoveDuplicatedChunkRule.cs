using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 删除重复的直播数据
    /// </summary>
    /// <remarks>
    /// 通过对比直播数据的特征，删除重复的直播数据<br/>
    /// <br/>
    /// 本规则为一般规则
    /// </remarks>
    public class RemoveDuplicatedChunkRule : ISimpleProcessingRule
    {
        private const int MAX_HISTORY = 8;
        private const string QUEUE_KEY = "DeDuplicationQueue";

        public Task RunAsync(FlvProcessingContext context, Func<Task> next)
        {
            if (!(context.OriginalInput is PipelineDataAction data))
                return next();
            else
            {
                var feature = new List<long>(data.Tags.Count * 2 + 1)
                {
                    data.Tags.Count
                };

                unchecked
                {
                    // 计算一个特征码
                    // 此处并没有遵循什么特定的算法，只是随便取了一些代表性比较强的值，用简单又尽量可靠的方式糅合到一起而已
                    foreach (var tag in data.Tags)
                    {
                        var f = 0L;
                        f ^= tag.Type switch
                        {
                            TagType.Audio => 0b01,
                            TagType.Video => 0b10,
                            TagType.Script => 0b11,
                            _ => 0b00,
                        };
                        f <<= 3;
                        f ^= (int)tag.Flag & ((1 << 3) - 1);
                        f <<= 32;
                        f ^= tag.Timestamp;
                        f <<= 32 - 5;
                        f ^= tag.Size & ((1 << (32 - 5)) - 1);
                        feature.Add(f);

                        if (tag.Nalus == null)
                            feature.Add(long.MinValue);
                        else
                        {
                            long n = tag.Nalus.Count << 32;
                            foreach (var nalu in tag.Nalus)
                                n ^= (((int)nalu.Type) << 16) ^ ((int)nalu.FullSize);
                            feature.Add(n);
                        }
                    }
                }

                // 存储最近 MAX_HISTORY 个 Data Chunk 的特征的 Queue
                Queue<List<long>> history;
                if (context.SessionItems.TryGetValue(QUEUE_KEY, out var obj) && obj is Queue<List<long>> q)
                    history = q;
                else
                {
                    history = new Queue<List<long>>(MAX_HISTORY + 1);
                    context.SessionItems[QUEUE_KEY] = history;
                }

                // 对比历史特征
                if (history.Any(x => x.SequenceEqual(feature)))
                {
                    context.ClearOutput();
                    context.AddComment("发现了重复的 Flv Chunk");
                    return Task.CompletedTask;
                }
                else
                {
                    history.Enqueue(feature);

                    while (history.Count > MAX_HISTORY)
                        history.Dequeue();

                    return next();
                }
            }
        }
    }
}
