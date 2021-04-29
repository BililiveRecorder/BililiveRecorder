using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

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

        public List<PipelineAction> Actions { get; set; }

        public IDictionary<object, object?> SessionItems { get; private set; }

        public IDictionary<object, object?> LocalItems { get; private set; }

        public List<ProcessingComment> Comments { get; private set; }

        public void Reset(PipelineAction action, IDictionary<object, object?> sessionItems)
        {
            var actions = new List<PipelineAction> { action ?? throw new ArgumentNullException(nameof(action)) };
            this.Reset(actions, sessionItems);
        }

        public void Reset(List<PipelineAction> actions, IDictionary<object, object?> sessionItems)
        {
            this.SessionItems = sessionItems ?? throw new ArgumentNullException(nameof(sessionItems));
            this.Actions = actions;
            this.LocalItems = new Dictionary<object, object?>();
            this.Comments = new List<ProcessingComment>();
        }
    }

    public static class FlvProcessingContextExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddComment(this FlvProcessingContext context, ProcessingComment comment)
            => context.Comments.Add(comment);

        public static bool PerActionRun(this FlvProcessingContext context, Func<FlvProcessingContext, PipelineAction, IEnumerable<PipelineAction?>> func)
        {
            var success = true;
            var actions = context.Actions;
            var result = new List<PipelineAction>();
            foreach (var output in actions.SelectMany(action => func(context, action)))
            {
                if (output is null)
                {
                    success = false;
                    goto exit;
                }
                result.Add(output);
            }

        exit:
            context.Actions = result;

            return success;
        }
    }
}
