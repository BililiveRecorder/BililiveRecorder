using System;
using System.Linq;
using BililiveRecorder.Web.Api;
using BililiveRecorder.Web.Models.Rest.Logs;
using Xunit;

namespace BililiveRecorder.Web.Tests
{
    public class LogControllerTests : IDisposable
    {
        public void Dispose()
        {
            WebApiLogEventSink.Instance = null;
        }

        [Fact]
        public void GetJsonLogReturnsEmptyDtoWhenSinkHasNoLogs()
        {
            WebApiLogEventSink.Instance = new WebApiLogEventSink(new WebApiLogEventSinkTests.TestFormatter());

            var result = new LogController().GetJsonLog(after: null).Value!;

            Assert.False(result.Continuous);
            Assert.Equal(0, result.Cursor);
            Assert.Empty(result.Logs);
        }

        [Fact]
        public void GetJsonLogReturnsMergedLogs()
        {
            var sink = new WebApiLogEventSink(new WebApiLogEventSinkTests.TestFormatter());
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Information, "info"));
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Debug, "debug"));
            WebApiLogEventSink.Instance = sink;

            var result = new LogController().GetJsonLog(after: null).Value!;

            Assert.Equal(2, result.Cursor);
            Assert.Equal(new[] { "Information:info", "Debug:debug" }, result.Logs.ToArray());
        }

        [Fact]
        public void GetJsonLogReturnsOnlyLogsAfterCursorWhenContiguous()
        {
            var sink = new WebApiLogEventSink(new WebApiLogEventSinkTests.TestFormatter());
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Information, "info-1"));
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Debug, "debug-2"));
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Information, "info-3"));
            WebApiLogEventSink.Instance = sink;

            var result = new LogController().GetJsonLog(after: 1).Value!;

            Assert.True(result.Continuous);
            Assert.Equal(3, result.Cursor);
            Assert.Equal(new[] { "Debug:debug-2", "Information:info-3" }, result.Logs.ToArray());
        }

        [Fact]
        public void GetJsonLogReturnsResetWhenDebugLogsAreEvictedAfterCursor()
        {
            var sink = new WebApiLogEventSink(new WebApiLogEventSinkTests.TestFormatter());
            sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Information, "info"));
            for (var i = 0; i < 101; i++)
            {
                sink.Emit(WebApiLogEventSinkTests.CreateLog(Serilog.Events.LogEventLevel.Debug, "debug-" + i));
            }
            WebApiLogEventSink.Instance = sink;

            var result = new LogController().GetJsonLog(after: 1).Value!;

            Assert.False(result.Continuous);
            Assert.Equal(102, result.Cursor);
            Assert.Equal(101, result.Logs.Count());
            Assert.Contains("Information:info", result.Logs);
            Assert.DoesNotContain("Debug:debug-0", result.Logs);
            Assert.Contains("Debug:debug-100", result.Logs);
        }
    }
}
