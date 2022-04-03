using Microsoft.AspNetCore.Mvc;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public class VersionController : ControllerBase
    {
        /// <summary>
        /// 读取软件版本信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Schemas.Types.RecorderVersion GetVersion() => Schemas.Types.RecorderVersion.Instance;
    }
}
