using System;
using System.Collections.Generic;
using System.Linq;

namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingPipelineBuilder : IProcessingPipelineBuilder
    {
        public IServiceProvider ServiceProvider { get; }

        private readonly List<Func<ProcessingDelegate, ProcessingDelegate>> rules = new List<Func<ProcessingDelegate, ProcessingDelegate>>();

        public ProcessingPipelineBuilder(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IProcessingPipelineBuilder Add(Func<ProcessingDelegate, ProcessingDelegate> rule)
        {
            this.rules.Add(rule);
            return this;
        }

        public ProcessingDelegate Build()
            => this.rules.AsEnumerable().Reverse().Aggregate((ProcessingDelegate)(_ => { }), (i, o) => o(i));
    }
}
