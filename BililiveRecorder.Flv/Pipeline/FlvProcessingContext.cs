using System;
using System.Collections.Generic;

namespace BililiveRecorder.Flv.Pipeline
{
    public class FlvProcessingContext
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FlvProcessingContext()
        {
        }

        public FlvProcessingContext(PipelineAction data, IDictionary<object, object?> sessionItems)
        {
            this.Reset(data, sessionItems);
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public PipelineAction OriginalInput { get; private set; }

        public List<PipelineAction> Output { get; set; }

        public IDictionary<object, object?> SessionItems { get; private set; }

        public IDictionary<object, object?> LocalItems { get; private set; }

        public List<string> Comments { get; private set; }

        public void Reset(PipelineAction data, IDictionary<object, object?> sessionItems)
        {
            this.OriginalInput = data ?? throw new ArgumentNullException(nameof(data));
            this.SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
            this.Output = new List<PipelineAction> { this.OriginalInput.Clone() };
            this.LocalItems = new Dictionary<object, object?>();
            this.Comments = new List<string>();
        }
    }

    public static class FlvProcessingContextExtensions
    {
        public static void AddComment(this FlvProcessingContext context, string comment)
            => context.Comments.Add(comment);

        public static void AddNewFileAtStart(this FlvProcessingContext context)
            => context.Output.Insert(0, PipelineNewFileAction.Instance);

        public static void AddNewFileAtEnd(this FlvProcessingContext context)
            => context.Output.Add(PipelineNewFileAction.Instance);

        public static void AddDisconnectAtStart(this FlvProcessingContext context)
            => context.Output.Insert(0, PipelineDisconnectAction.Instance);

        public static void ClearOutput(this FlvProcessingContext context)
            => context.Output.Clear();
    }
}
