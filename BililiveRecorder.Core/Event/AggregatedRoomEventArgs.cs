using System;

namespace BililiveRecorder.Core.Event
{
    public sealed class AggregatedRoomEventArgs<T>
    {
        public AggregatedRoomEventArgs(IRoom room, T @event)
        {
            this.Room = room ?? throw new ArgumentNullException(nameof(room));
            this.Event = @event;
        }

        public IRoom Room { get; }

        public T Event { get; }
    }
}
