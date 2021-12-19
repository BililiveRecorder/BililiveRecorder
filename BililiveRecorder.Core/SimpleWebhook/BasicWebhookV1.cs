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
    public class BasicWebhookV1
    {
        private static readonly ILogger logger = Log.ForContext<BasicWebhookV1>();
        private static readonly HttpClient client;

        private readonly ConfigV3 Config;

        static BasicWebhookV1()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", $"BililiveRecorder/{GitVersionInformation.FullSemVer}");
        }

        public BasicWebhookV1(ConfigV3 config)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task SendAsync(RecordEndData data)
        {
            var urls = this.Config.Global.WebHookUrls;
            if (string.IsNullOrWhiteSpace(urls)) return;

            var dataStr = JsonConvert.SerializeObject(data, Formatting.None);
            using var body = new ByteArrayContent(Encoding.UTF8.GetBytes(dataStr));
            body.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var tasks = urls!
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => this.SendImplAsync(x, body));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task SendImplAsync(string url, HttpContent data)
        {
            for (var i = 0; i < 3; i++)
                try
                {
                    var result = await client.PostAsync(url, data).ConfigureAwait(false);
                    result.EnsureSuccessStatusCode();
                    return;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "发送 Webhook 到 {Url} 失败", url);
                }
        }
    }
}
