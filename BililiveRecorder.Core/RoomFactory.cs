using System;
using BililiveRecorder.Core.Config.V3;
using Microsoft.Extensions.DependencyInjection;

namespace BililiveRecorder.Core
{
    internal class RoomFactory : IRoomFactory
    {
        private readonly IServiceProvider serviceProvider;

        public RoomFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IRoom CreateRoom(RoomConfig roomConfig, int initDelayFactor)
        {
            var scope = this.serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            return ActivatorUtilities.CreateInstance<Room>(sp, scope, roomConfig, initDelayFactor);
        }
    }
}
