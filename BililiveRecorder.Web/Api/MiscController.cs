using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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
        public ActionResult<FileNameTemplateOutput> GenerateFileName([FromBody] GenerateFileNameInput input)
        {
            var config = new GlobalConfig()
            {
                WorkDirectory = "/",
                FileNameRecordTemplate = input.Template
            };
            var generator = new FileNameGenerator(config, null);

            var context = this.mapper.Map<FileNameTemplateContext>(input.Context);

            var output = generator.CreateFilePath(context);
            return output;
        }

        /// <summary>
        /// 获取可用的网络接口列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("networkInterfaces")]
        public ActionResult<List<NetworkInterfaceDto>> GetNetworkInterfaces()
        {
            try
            {
                var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .Select(ni =>
                    {
                        var properties = ni.GetIPProperties();
                        var addresses = properties.UnicastAddresses
                            .Select(addr => addr.Address.ToString())
                            .ToList();

                        return new NetworkInterfaceDto
                        {
                            Name = ni.Name,
                            Description = ni.Description,
                            NetworkInterfaceType = ni.NetworkInterfaceType.ToString(),
                            Addresses = addresses,
                        };
                    })
                    .ToList();

                return interfaces;
            }
            catch (Exception)
            {
                return new List<NetworkInterfaceDto>();
            }
        }
    }

    public class NetworkInterfaceDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NetworkInterfaceType { get; set; } = string.Empty;
        public List<string> Addresses { get; set; } = new();
    }
}
