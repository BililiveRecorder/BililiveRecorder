using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BililiveRecorder.Flv;
using BililiveRecorder.Flv.Pipeline.Actions;
using FastHashes;
using StructLinq;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    /// <summary>
    /// 删除重复的直播数据。
    /// </summary>
    public class RemoveDuplicatedChunkRule : ISimpleProcessingRule
    {
        private const int MAX_HISTORY = 16;
        private const string QUEUE_KEY = "DeDuplicationQueue";
        private const string DUPLICATED_COUNT_KEY = "DuplicatedFlvDataCount";

        private static readonly FarmHash64 farmHash64 = new();
        private static readonly ProcessingComment comment = new ProcessingComment(CommentType.RepeatingData, true, "重复数据");

        public void Run(FlvProcessingContext context, Action next)
        {
            context.PerActionRun(this.RunPerAction);
            next();
        }

        private IEnumerable<PipelineAction?> RunPerAction(FlvProcessingContext context, PipelineAction action)
        {
            if (action is PipelineDataAction data)
            {
                var tagHashs = new MemoryStream(4 + data.Tags.Count * 16);

                {
                    var buffer = ArrayPool<byte>.Shared.Rent(4);
                    try
                    {
                        BinaryPrimitives.WriteInt32BigEndian(buffer, data.Tags.Count);
                        tagHashs.Write(buffer, 0, 4);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                foreach (var tag in data.Tags)
                {
                    var tagHash = tag.DataHash ?? tag.UpdateDataHash();
                    if (tagHash is not null)
                    {
                        var bytes = Encoding.UTF8.GetBytes(tagHash);
                        tagHashs.Write(bytes, 0, bytes.Length);
                    }
                }

                var hash = farmHash64.ComputeHash(tagHashs.GetBuffer(), (int)tagHashs.Length);

                // 存储最近 MAX_HISTORY 个 Data Chunk 的特征的 Queue
                Queue<byte[]> hashHistory;
                if (context.SessionItems.TryGetValue(QUEUE_KEY, out var obj) && obj is Queue<byte[]> q)
                    hashHistory = q;
                else
                {
                    hashHistory = new Queue<byte[]>(MAX_HISTORY + 1);
                    context.SessionItems[QUEUE_KEY] = hashHistory;
                }

                // 对比历史特征
                if (hashHistory.ToStructEnumerable().Any(x => x.SequenceEqual(hash), x => x))
                {
                    // 重复数据                    
                    context.AddComment(comment);

                    // 判断连续收到的重复数据数量
                    if (context.SessionItems.ContainsKey(DUPLICATED_COUNT_KEY) && context.SessionItems[DUPLICATED_COUNT_KEY] is int count)
                    {
                        count += 1;
                    }
                    else
                    {
                        count = 1;
                    }

                    const int DisconnectOnDuplicatedDataCount = 10;
                    if (count > DisconnectOnDuplicatedDataCount)
                    {
                        yield return new PipelineDisconnectAction($"连续收到了 {DisconnectOnDuplicatedDataCount} 段重复数据");
                        context.SessionItems.Remove(DUPLICATED_COUNT_KEY);
                    }
                    else
                    {
                        context.SessionItems[DUPLICATED_COUNT_KEY] = count;
                    }
                }
                else
                {
                    // 新数据
                    hashHistory.Enqueue(hash);

                    while (hashHistory.Count > MAX_HISTORY)
                        hashHistory.Dequeue();

                    context.SessionItems.Remove(DUPLICATED_COUNT_KEY);
                    yield return action;
                }
            }
            else
            {
                yield return action;
            }
        }
    }
}
