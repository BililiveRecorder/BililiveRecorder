using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Writer;
using BililiveRecorder.Flv.Xml;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public abstract class TestBase
    {
        protected XmlFlvFile LoadFile(string path) =>
            (XmlFlvFile)XmlFlvFile.Serializer.Deserialize(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));

        protected ProcessingDelegate BuildPipeline() =>
            new ProcessingPipelineBuilder(new ServiceCollection().BuildServiceProvider()).AddDefault().AddRemoveFillerData().Build();

        protected async Task RunPipeline(ITagGroupReader reader, IFlvTagWriter output, List<ProcessingComment> comments)
        {
            var writer = new FlvProcessingContextWriter(output);
            var session = new Dictionary<object, object?>();
            var context = new FlvProcessingContext();
            var pipeline = this.BuildPipeline();

            while (true)
            {
                var group = await reader.ReadGroupAsync(default).ConfigureAwait(false);

                if (group is null)
                    break;

                context.Reset(group, session);
                await pipeline(context).ConfigureAwait(false);

                comments.AddRange(context.Comments);
                await writer.WriteAsync(context).ConfigureAwait(false);
            }
        }
    }
}
