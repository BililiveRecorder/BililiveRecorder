using System;
using System.Collections.Generic;
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
    internal class BasicWebhookV2
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

        public Task SendStreamStartedAsync(StreamStartedEventArgs args) =>
            this.SendAsync(new EventWrapper<StreamStartedEventArgs>(args) { EventType = EventType.StreamStarted });

        public Task SendStreamEndedAsync(StreamEndedEventArgs args) =>
            this.SendAsync(new EventWrapper<StreamEndedEventArgs>(args) { EventType = EventType.StreamEnded });

        private async Task SendAsync(object data)
        {
            var urls = this.config.WebHookUrlsV2;

            if (string.IsNullOrWhiteSpace(urls))
                return;

            var dataStr = JsonConvert.SerializeObject(data, Formatting.None);

            logger.Debug("尝试发送 WebhookV2 到 {Urls}, 数据 {WebhookData}", urls, dataStr);

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
            if (!IsUrlAllowed(url))
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
                    logger.Debug("发送 WebhookV2 到 {Url} 成功", url);
                    return;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "发送 WebhookV2 到 {Url} 失败", url);
                }
        }

        private static readonly IReadOnlyList<string> DisallowedDomains = new[]
        {
            "test.example.com",
            "baidu" + ".com",
            "qq" + ".com",
            "google" + ".com",
            "b23" + ".tv",
            "bilibili" + ".com",
            "bilibili" + ".cn",
            "bilibili" + ".tv",
            "bilicomic" + ".com",
            "bilicomics" + ".com",
            "bilivideo" + ".com",
            "bilivideo" + ".cn",
            "biligame" + ".com",
            "biligame" + ".net",
            "biliapi" + ".com",
            "biliapi" + ".net",
            "hdslb" + ".com",
        };

        internal static bool IsUrlAllowed(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            foreach (var domain in DisallowedDomains)
                if (uri.Host.EndsWith(domain, StringComparison.OrdinalIgnoreCase))
                    return false;

            return true;
        }
    }
}
