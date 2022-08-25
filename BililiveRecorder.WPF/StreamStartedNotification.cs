using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BililiveRecorder.Core;
using Microsoft.Toolkit.Uwp.Notifications;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal static class StreamStartedNotification
    {
        public static async Task ShowAsync(IRoom room)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "brec-notifi");
            Uri? cover = null, face = null;
            DateTime? time = null;
            try
            {
                Directory.CreateDirectory(tempPath);

                var json = room.RawBilibiliApiJsonData;

                var live_start_time = json?["room_info"]?["live_start_time"]?.ToObject<long?>();
                if (live_start_time.HasValue && live_start_time > 0)
                {
                    time = DateTimeOffset.FromUnixTimeSeconds(live_start_time.Value).LocalDateTime;
                }

                var coverUrl = json?["room_info"]?["cover"]?.ToObject<string>();
                var faceUrl = json?["anchor_info"]?["base_info"]?["face"]?.ToObject<string>();

                var coverFile = Path.Combine(tempPath, Path.GetFileName(coverUrl));
                var faceFile = Path.Combine(tempPath, Path.GetFileName(faceUrl));

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.UserAgent.Clear();

                if (!string.IsNullOrEmpty(faceUrl))
                {
                    using var faceFs = new FileStream(faceFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await (await client.GetStreamAsync(faceUrl).ConfigureAwait(false)).CopyToAsync(faceFs).ConfigureAwait(false);
                    face = new Uri(faceFile);
                }

                if (!string.IsNullOrWhiteSpace(coverUrl))
                {
                    using var coverFs = new FileStream(coverFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await (await client.GetStreamAsync(coverUrl).ConfigureAwait(false)).CopyToAsync(coverFs).ConfigureAwait(false);
                    cover = new Uri(coverFile);
                }
            }
            catch (Exception)
            { }

            var roomUrl = new Uri("https://live.bilibili.com/" + room.RoomConfig.RoomId);
            var builder = new ToastContentBuilder()
                .AddHeader("BililiveRecorder-StreamStarted", "B站录播姬开播通知", "")
                .AddText(room.Name + " 开播了")
                .AddText(room.Title)
                .AddText($"{room.AreaNameParent} · {room.AreaNameChild}")
                .SetProtocolActivation(roomUrl)
                .SetToastDuration(ToastDuration.Long)
                .AddButton(new ToastButton().SetContent("打开直播间").SetProtocolActivation(roomUrl))
                ;

            if (time.HasValue)
                builder.AddCustomTimeStamp(time.Value);

            if (face is not null)
                builder.AddAppLogoOverride(face, ToastGenericAppLogoCrop.Circle);

            if (cover is not null)
                builder.AddInlineImage(cover);

            builder.Show();
        }
    }
}
