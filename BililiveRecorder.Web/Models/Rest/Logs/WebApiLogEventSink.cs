using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace BililiveRecorder.Web.Models.Rest.Logs
{
    public class WebApiLogEventSink : ILogEventSink
    {
        public static WebApiLogEventSink? Instance;

        private const int MAX_LOG = 100;

        private readonly ReaderWriterLockSlim readerWriterLock = new();
        private readonly ITextFormatter textFormatter;

        private readonly Queue<JsonLog> logs = new Queue<JsonLog>();
        private readonly Queue<JsonLog> debugLogs = new Queue<JsonLog>();

        private int logId = 0;

        public WebApiLogEventSink(ITextFormatter textFormatter)
        {
            this.textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            this.textFormatter.Format(logEvent, writer);
            var json = writer.ToString();

            if (this.readerWriterLock.TryEnterWriteLock(5000))
            {
                try
                {
                    var queue = logEvent.Level == LogEventLevel.Debug ? this.debugLogs : this.logs;
                    queue.Enqueue(new JsonLog { Id = Interlocked.Increment(ref this.logId), Log = json });

                    while (queue.Count > MAX_LOG)
                        queue.Dequeue();
                }
                finally
                {
                    this.readerWriterLock.ExitWriteLock();
                }
            }
        }

        public void ReadLogs(Action<List<JsonLog>> callback)
        {
            if (this.readerWriterLock.TryEnterReadLock(1000))
            {
                try
                {
                    callback(this.logs.Concat(this.debugLogs).OrderBy(x => x.Id).ToList());
                }
                finally
                {
                    this.readerWriterLock.ExitReadLock();
                }
            }
        }
    }
}
