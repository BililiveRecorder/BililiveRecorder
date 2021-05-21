using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Flv.Grouping;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using BililiveRecorder.Flv.Writer;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BililiveRecorder.Flv.UnitTests.Grouping
{
    public class GroupingTest
    {
        private const string TEST_OUTPUT_PATH = @"";

        public class TestOutputProvider : IFlvWriterTargetProvider
        {
            public Stream CreateAlternativeHeaderStream() => throw new NotImplementedException();
            public (Stream, object?) CreateOutputStream() => (File.Open(Path.Combine(TEST_OUTPUT_PATH, DateTimeOffset.Now.ToString("s").Replace(':', '-') + ".flv"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None), null);
        }

        [Fact(Skip = "Not ready")]
        public async Task Test1Async()
        {
            var path = @"";

            var results = new List<PipelineAction>();

            var grouping = new TagGroupReader(new FlvTagPipeReader(PipeReader.Create(File.OpenRead(path)), new TestRecyclableMemoryStreamProvider(), skipData: true, logger: null));

            var context = new FlvProcessingContext();
            var session = new Dictionary<object, object?>();

            var sp = new ServiceCollection().BuildServiceProvider();
            var pipeline = new ProcessingPipelineBuilder(sp).AddDefault().AddRemoveFillerData().Build();

            while (true)
            {
                var g = await grouping.ReadGroupAsync(default).ConfigureAwait(false);

                if (g is null)
                    break;

                context.Reset(g, session);

                pipeline(context);

                foreach (var item in context.Actions)
                {
                    results.Add(item);
                }
            }

            var sizes = results.Select(a => a switch
            {
                PipelineDataAction x => x.Tags.Sum(b => b.Size),
                PipelineHeaderAction x => x.AllTags.Sum(b => b.Size),
                PipelineScriptAction x => x.Tag.Size,
                _ => 0,
            }
            ).ToArray();
        }

        [Fact(Skip = "Not ready")]
        public async Task Test2Async()
        {
            const string PATH = @"";

            using var grouping = new TagGroupReader(new FlvTagPipeReader(PipeReader.Create(File.OpenRead(PATH)), new TestRecyclableMemoryStreamProvider(), skipData: false, logger: null));

            var comments = new List<string>();

            var context = new FlvProcessingContext();
            var session = new Dictionary<object, object?>();

            var sp = new ServiceCollection().BuildServiceProvider();
            var pipeline = new ProcessingPipelineBuilder(sp).AddDefault().AddRemoveFillerData().Build();

            using var writer = new FlvProcessingContextWriter(tagWriter: new FlvTagFileWriter(new TestOutputProvider(), new TestRecyclableMemoryStreamProvider(), null), allowMissingHeader: false, disableKeyframes: true);

            while (true)
            {
                var g = await grouping.ReadGroupAsync(default).ConfigureAwait(false);

                if (g is null)
                    break;

                context.Reset(g, session);

                pipeline(context);

                comments.AddRange(context.Comments.Select(x => x.C));
                await writer.WriteAsync(context).ConfigureAwait(false);

                foreach (var action in context.Actions)
                {
                    // TODO action.Dispose();

                    if (action is PipelineDataAction dataAction)
                    {
                        foreach (var tag in dataAction.Tags)
                        {
                            tag.BinaryData?.Dispose();
                        }
                    }
                }
            }
        }
    }
}
