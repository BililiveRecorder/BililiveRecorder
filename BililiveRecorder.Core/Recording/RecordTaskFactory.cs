using System;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Core.Recording
{
    public class RecordTaskFactory : IRecordTaskFactory
    {
        private readonly IServiceProvider serviceProvider;

        public RecordTaskFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IRecordTask CreateRecordTask(IRoom room) =>
            ActivatorUtilities.CreateInstance<RecordTask>(this.serviceProvider, room);
    }
}
