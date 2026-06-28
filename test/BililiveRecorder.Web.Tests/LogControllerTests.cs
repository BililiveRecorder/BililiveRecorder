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
    }
}
