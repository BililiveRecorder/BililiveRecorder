using System;
using BililiveRecorder.Core.Config.V2;
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
            room.RoomConfig.RecordMode switch
            {
                RecordMode.RawData => ActivatorUtilities.CreateInstance<RawDataRecordTask>(this.serviceProvider, room),
                _ => ActivatorUtilities.CreateInstance<StandardRecordTask>(this.serviceProvider, room)
            };
    }
}
