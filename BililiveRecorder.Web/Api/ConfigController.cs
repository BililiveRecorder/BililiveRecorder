using System;
using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Web.Models;
using BililiveRecorder.Web.Models.Rest;
using Microsoft.AspNetCore.Mvc;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public class ConfigController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IRecorder recorder;

        public ConfigController(IMapper mapper, IRecorder recorder)
        {
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        }

        /// <summary>
        /// 获取软件默认设置
        /// </summary>
        /// <returns></returns>
        [HttpGet("default")]
        public ActionResult<DefaultConfig> GetDefaultConfig() => DefaultConfig.Instance;

        /// <summary>
        /// 获取全局设置
        /// </summary>
        /// <returns></returns>
        [HttpGet("global")]
        public ActionResult<GlobalConfigDto> GetGlobalConfig() => this.mapper.Map<GlobalConfigDto>(this.recorder.Config.Global);

        /// <summary>
        /// 设置全局设置
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        [HttpPost("global")]
        public ActionResult<GlobalConfigDto> SetGlobalConfig([FromBody] SetGlobalConfig config)
        {
            config.ApplyTo(this.recorder.Config.Global);
            return this.mapper.Map<GlobalConfigDto>(this.recorder.Config.Global);
        }
    }
}
