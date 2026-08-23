using System.Collections.Generic;
using System.Linq;
using BililiveRecorder.Web.Models.Rest.Logs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StructLinq;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public sealed class LogController : ControllerBase
    {
        public LogController()
        {
        }

        /// <summary>
        /// 获取 JSON 日志
        /// </summary>
        /// <param name="after">只获取此 id 之后的日志</param>
        /// <returns></returns>
        [HttpGet("fetch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<JsonLogDto> GetJsonLog([FromQuery] long? after)
        {
            var sink = WebApiLogEventSink.Instance;
            if (sink is null)
            {
                return new JsonLogDto();
            }

            var logs = new List<JsonLog>();

            sink.ReadLogs(items =>
            {
                logs = items;
            });

            if (logs.Count == 0)
            {
                return new JsonLogDto();
            }

            if (!after.HasValue)
            {
                return new JsonLogDto
                {
                    Continuous = false,
                    Cursor = logs[^1].Id,
                    Logs = logs.Select(x => x.Log)
                };
            }
            else
            {
                var index = logs.BinarySearch(new JsonLog { Id = after.Value });
                var continuous = index >= 0;

                if (continuous)
                {
                    for (var i = index; i < logs.Count - 1; i++)
                    {
                        if (logs[i].Id + 1 != logs[i + 1].Id)
                        {
                            continuous = false;
                            break;
                        }
                    }
                }

                return new JsonLogDto
                {
                    Continuous = continuous,
                    Cursor = logs[^1].Id,
                    Logs = logs.TakeLast(continuous ? logs.Count - index - 1 : logs.Count).Select(x => x.Log)
                };
            }
        }
    }
}
