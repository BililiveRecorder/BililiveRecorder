using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using Newtonsoft.Json;
using Serilog;

namespace BililiveRecorder.Core.SimpleWebhook
{
    internal class BasicWebhookV1
    {
        private static readonly ILogger logger = Log.ForContext<BasicWebhookV1>();

        private readonly HttpClient client;
        private readonly ConfigV3 config;

        public BasicWebhookV1(ConfigV3 config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Add("User-Agent", $"BililiveRecorder/{GitVersionInformation.FullSemVer}");
        }

        public async Task SendAsync(RecordEndData data)
        {
            var urls = this.config.Global.WebHookUrls;
            if (string.IsNullOrWhiteSpace(urls))
                return;

            var dataStr = JsonConvert.SerializeObject(data, Formatting.None);

            logger.Debug("尝试发送 WebhookV1 数据 {WebhookData}, 地址 {Urls}", dataStr, urls);

            var bytes = Encoding.UTF8.GetBytes(dataStr);

            var tasks = urls!
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => this.SendImplAsync(x, bytes));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task SendImplAsync(string url, byte[] data)
        {
            if (!BasicWebhookV2.IsUrlAllowed(url))
            {
                logger.Warning("不支持向 {Url} 发送 Webhook，已跳过", url);
                return;
            }

            for (var i = 0; i < 3; i++)
                try
                {
                    if (i > 0)
                        await Task.Delay(i * 1000);

                    using var body = new ByteArrayContent(data);
                    body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var result = await this.client.PostAsync(url, body).ConfigureAwait(false);
                    result.EnsureSuccessStatusCode();
                    logger.Debug("发送 WebhookV1 到 {Url} 成功", url);
                    return;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "发送 WebhookV1 到 {Url} 失败", url);
                }
        }
    }
}
