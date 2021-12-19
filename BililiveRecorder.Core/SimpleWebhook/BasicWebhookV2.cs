using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Event;
using Newtonsoft.Json;
using Serilog;

namespace BililiveRecorder.Core.SimpleWebhook
{
    public class BasicWebhookV2
    {
        private static readonly ILogger logger = Log.ForContext<BasicWebhookV2>();

        private readonly HttpClient client;
        private readonly GlobalConfig config;

        public BasicWebhookV2(GlobalConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            this.client = new HttpClient();
            this.client.DefaultRequestHeaders.Add("User-Agent", $"BililiveRecorder/{GitVersionInformation.FullSemVer}");
        }

        public Task SendSessionStartedAsync(RecordSessionStartedEventArgs args) =>
            this.SendAsync(new EventWrapper<RecordSessionStartedEventArgs>(args) { EventType = EventType.SessionStarted });

        public Task SendSessionEndedAsync(RecordSessionEndedEventArgs args) =>
            this.SendAsync(new EventWrapper<RecordSessionEndedEventArgs>(args) { EventType = EventType.SessionEnded });

        public Task SendFileOpeningAsync(RecordFileOpeningEventArgs args) =>
            this.SendAsync(new EventWrapper<RecordFileOpeningEventArgs>(args) { EventType = EventType.FileOpening });

        public Task SendFileClosedAsync(RecordFileClosedEventArgs args) =>
            this.SendAsync(new EventWrapper<RecordFileClosedEventArgs>(args) { EventType = EventType.FileClosed });

        private async Task SendAsync(object data)
        {
            var urls = this.config.WebHookUrlsV2;
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
                    var result = await this.client.PostAsync(url, data).ConfigureAwait(false);
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
