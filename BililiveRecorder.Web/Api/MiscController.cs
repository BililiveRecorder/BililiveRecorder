using System;
using AutoMapper;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Templating;
using BililiveRecorder.Web.Models.Rest;
using Microsoft.AspNetCore.Mvc;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public sealed class MiscController : ControllerBase
    {
        private readonly IMapper mapper;

        public MiscController(IMapper mapper)
        {
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// 根据传入参数生成录播文件名
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost("generateFileName")]
        public ActionResult<string> GenerateFileName([FromBody] GenerateFileNameInput input)
        {
            var config = new GlobalConfig()
            {
                WorkDirectory = "/",
                FileNameRecordTemplate = input.Template
            };
            var generator = new FileNameGenerator(config);

            var context = this.mapper.Map<FileNameTemplateContext>(input.Context);

            var (_, relativePath) = generator.CreateFilePath(context);
            return relativePath;
        }
    }
}
