using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BililiveRecorder.Core;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal static class StreamStartedNotification
    {
        private static readonly ILogger logger = Log.ForContext(typeof(StreamStartedNotification));

        private static readonly INotificationApi notificationApi;

        static StreamStartedNotification()
        {
            try
            {
                notificationApi = new NotificationCenterApi();
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "构造 NotificationCenterApi 时发生错误");
                notificationApi = new BalloonTipApi();
            }

            logger.Debug("使用通知API: {NotificationApi}", notificationApi.GetType().Name);
        }

        internal static Task ShowAsync(IRoom room) => notificationApi.ShowAsync(room);

        internal static void Cleanup() => notificationApi.Cleanup();


        internal interface INotificationApi
        {
            Task ShowAsync(IRoom room);
            void Cleanup();
        }

        internal class BalloonTipApi : INotificationApi
        {
            public void Cleanup() { }

            public Task ShowAsync(IRoom room)
            {
                _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (Application.Current.MainWindow is NewMainWindow nmw)
                    {
                        nmw.ShowBalloonTipCallback?.Invoke(room.Name + " 开播了", $"{room.Title}\n{room.AreaNameParent} · {room.AreaNameChild}", BalloonIcon.None);
                    }
                }));

                return Task.CompletedTask;
            }
        }

        internal class NotificationCenterApi : INotificationApi
        {
            private readonly string tempPath;

            public NotificationCenterApi()
            {
                throw new NotImplementedException("TODO: re-support windows notification center");
                // FIXME: _ = ToastNotificationManagerCompat.History;

                this.tempPath = Path.Combine(Path.GetTempPath(), "brec-notifi", Process.GetCurrentProcess().Id.ToString());

                try
                {
                    Directory.CreateDirectory(this.tempPath);
                }
                catch (Exception)
                {
                    try
                    {
                        Directory.CreateDirectory(this.tempPath);
                    }
                    catch (Exception)
                    { }
                }
            }

            public void Cleanup()
            {
                // FIXME: ToastNotificationManagerCompat.Uninstall();

                try
                {
                    Directory.Delete(path: this.tempPath, recursive: true);
                }
                catch (Exception)
                {
                    try
                    {
                        Directory.Delete(path: this.tempPath, recursive: true);
                    }
                    catch (Exception)
                    { }
                }
            }

            public async Task ShowAsync(IRoom room)
            {
                Uri? cover = null, face = null;
                DateTime? time = null;
                try
                {
                    var json = room.RawBilibiliApiJsonData;

                    var live_start_time = json?["room_info"]?["live_start_time"]?.ToObject<long?>();
                    if (live_start_time.HasValue && live_start_time > 0)
                    {
                        time = DateTimeOffset.FromUnixTimeSeconds(live_start_time.Value).LocalDateTime;
                    }

                    var coverUrl = json?["room_info"]?["cover"]?.ToObject<string>();
                    var faceUrl = json?["anchor_info"]?["base_info"]?["face"]?.ToObject<string>();

                    var coverFile = Path.Combine(this.tempPath, Path.GetFileName(coverUrl));
                    var faceFile = Path.Combine(this.tempPath, Path.GetFileName(faceUrl));

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
                    .AddHeader("BililiveRecorder-StreamStarted", "录播姬开播通知", "")
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

                // FIXME: builder.Show();
            }
        }
    }
}
