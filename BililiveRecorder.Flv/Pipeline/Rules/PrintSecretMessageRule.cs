using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BililiveRecorder.Flv.Pipeline.Actions;
using Serilog;

namespace BililiveRecorder.Flv.Pipeline.Rules
{
    public class PrintSecretMessageRule : ISimpleProcessingRule
    {
        private static readonly ILogger logger = Log.ForContext<PrintSecretMessageRule>();

        public void Run(FlvProcessingContext context, Action next)
        {
            for (var i = 0; i < context.Actions.Count; i++)
            {
                if (context.Actions[i] is PipelineDataAction dataAction)
                {
                    var t = dataAction.Tags.Where(x => x.IsKeyframeData()).FirstOrDefault();
                    if (t is not null && t.Nalus is not null && t.BinaryData is not null)
                    {
                        for (var i1 = 0; i1 < t.Nalus.Count; i1++)
                        {
                            var nalu = t.Nalus[i1];
                            if (nalu.Type == H264NaluType.Sei)
                            {
                                ReadMessage(nalu, t.BinaryData);
                            }
                        }
                    }
                }
            }
            next();
        }

        private static ReadOnlySpan<byte> Identifier => new byte[] {
            0x3f, 0xb3, 0x4b, (byte)'g', (byte)'e', (byte)'n', (byte)'t',(byte)'e',
           (byte)'u',(byte)'r',(byte)'e',0x9c,0x67,0xe3,0x7d,0x8c
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReadMessage(H264Nalu nalu, Stream stream)
        {
            var size = 0;
            var id = Identifier;

            stream.Seek(nalu.StartPosition + 1, SeekOrigin.Begin);
            if (stream.ReadByte() != 5)
                return;

            while (true)
            {
                var read = stream.ReadByte();
                size += read;
                if (read != 255)
                    break;
            }

            var buffer = new byte[id.Length];
            stream.Read(buffer, 0, buffer.Length);
            if (!id.SequenceEqual(buffer))
                return;

            var data = new byte[size - id.Length];
            stream.Read(data, 0, data.Length);
            var str = Encoding.UTF8.GetString(data);

            logger.Information("从直播数据中读到了神秘消息: {Message}", str);
        }
    }
}
