using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BililiveRecorder.Web.Models.Rest.Logs;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Parsing;
using Xunit;

namespace BililiveRecorder.Web.Tests
{
    public class WebApiLogEventSinkTests
    {
        [Fact]
        public void KeepsNonDebugLogsWhenDebugLogsOverflow()
        {
            var sink = new WebApiLogEventSink(new TestFormatter());

            sink.Emit(CreateLog(LogEventLevel.Information, "info"));
            for (var i = 0; i < 101; i++)
            {
                sink.Emit(CreateLog(LogEventLevel.Debug, "debug-" + i));
            }

            var logs = ReadLogs(sink);

            Assert.Equal(101, logs.Count);
            Assert.Contains(logs, x => x.Log == "Information:info");
            Assert.DoesNotContain(logs, x => x.Log == "Debug:debug-0");
            Assert.Contains(logs, x => x.Log == "Debug:debug-100");
        }

        [Fact]
        public void KeepsDebugLogsWhenNonDebugLogsOverflow()
        {
            var sink = new WebApiLogEventSink(new TestFormatter());

            sink.Emit(CreateLog(LogEventLevel.Debug, "debug"));
            for (var i = 0; i < 101; i++)
            {
                sink.Emit(CreateLog(LogEventLevel.Warning, "warning-" + i));
            }

            var logs = ReadLogs(sink);

            Assert.Equal(101, logs.Count);
            Assert.Contains(logs, x => x.Log == "Debug:debug");
            Assert.DoesNotContain(logs, x => x.Log == "Warning:warning-0");
            Assert.Contains(logs, x => x.Log == "Warning:warning-100");
        }

        [Fact]
        public void MergesLogsById()
        {
            var sink = new WebApiLogEventSink(new TestFormatter());

            sink.Emit(CreateLog(LogEventLevel.Information, "info-1"));
            sink.Emit(CreateLog(LogEventLevel.Debug, "debug"));
            sink.Emit(CreateLog(LogEventLevel.Information, "info-2"));

            var logs = ReadLogs(sink);

            Assert.Equal(new[] { "Information:info-1", "Debug:debug", "Information:info-2" }, logs.Select(x => x.Log));
            Assert.Equal(new long[] { 1, 2, 3 }, logs.Select(x => x.Id));
        }

        private static List<JsonLog> ReadLogs(WebApiLogEventSink sink)
        {
            var logs = new List<JsonLog>();
            sink.ReadLogs(items => logs = items);
            return logs;
        }

        internal static LogEvent CreateLog(LogEventLevel level, string message)
        {
            return new LogEvent(
                DateTimeOffset.Now,
                level,
                exception: null,
                new MessageTemplateParser().Parse(message),
                Array.Empty<LogEventProperty>());
        }

        internal class TestFormatter : ITextFormatter
        {
            public void Format(LogEvent logEvent, TextWriter output)
            {
                output.Write(logEvent.Level);
                output.Write(":");
                output.Write(logEvent.RenderMessage());
            }
        }
    }
}
