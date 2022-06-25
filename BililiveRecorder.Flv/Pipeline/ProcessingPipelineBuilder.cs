using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BililiveRecorder.Flv.Pipeline
{
    public class ProcessingPipelineBuilder : IProcessingPipelineBuilder
    {
        public IServiceCollection ServiceCollection { get; }

        private readonly List<Func<ProcessingDelegate, IServiceProvider, ProcessingDelegate>> rules = new();

        public ProcessingPipelineBuilder() : this(new ServiceCollection())
        { }

        public ProcessingPipelineBuilder(IServiceCollection servicesCollection)
        {
            this.ServiceCollection = servicesCollection;
        }

        public IProcessingPipelineBuilder AddRule(Func<ProcessingDelegate, IServiceProvider, ProcessingDelegate> rule)
        {
            this.rules.Add(rule);
            return this;
        }

        public ProcessingDelegate Build()
        {
            this.ServiceCollection.TryAddSingleton(_ => new ProcessingPipelineSettings());
            var provider = this.ServiceCollection.BuildServiceProvider();
            return this.rules.AsEnumerable().Reverse().Aggregate((ProcessingDelegate)(_ => { }), (i, o) => o(i, provider));
        }
    }
}
