using System;

namespace BililiveRecorder.Core.SimpleWebhook
{
    internal class EventWrapper<T> where T : class
    {
        public EventWrapper()
        {
        }

        public EventWrapper(T data)
        {
            this.EventData = data;
        }

        public EventType EventType { get; set; }

        public DateTimeOffset EventTimestamp { get; set; } = DateTimeOffset.Now;

        public Guid EventId { get; set; } = Guid.NewGuid();

        public T? EventData { get; set; }
    }
}
