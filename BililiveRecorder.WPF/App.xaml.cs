using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using NLog;
using Squirrel;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private void CheckUpdate(object sender, StartupEventArgs e)
        {
            logger.Debug($"Starting. FileV:{typeof(App).Assembly.GetName().Version.ToString(4)}, BuildV:{BuildInfo.Version}, Hash:{BuildInfo.HeadSha1}");
            logger.Debug("Environment.CommandLine: " + Environment.CommandLine);
            logger.Debug("Environment.CurrentDirectory: " + Environment.CurrentDirectory);
#if !DEBUG
            Task.Run(RunCheckUpdate);
#endif
        }

        private async Task RunCheckUpdate()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BILILIVE_RECORDER_DISABLE_UPDATE"))
                    || File.Exists("BILILIVE_RECORDER_DISABLE_UPDATE"))
                {
                    return;
                }

                var envPath = Environment.GetEnvironmentVariable("BILILIVE_RECORDER_OVERWRITE_UPDATE");
                var serverUrl = @"https://soft.danmuji.org/BililiveRecorder/";
                if (!string.IsNullOrWhiteSpace(envPath)) { serverUrl = envPath; }
                logger.Debug("Checking updates.");
                using (var manager = new UpdateManager(urlOrPath: serverUrl))
                {
                    var update = await manager.CheckForUpdate();
                    if (update.CurrentlyInstalledVersion == null)
                    {
                        logger.Debug("Squirrel 无当前版本");
                    }

                    if (!update.ReleasesToApply.Any())
                    {
                        logger.Info($@"当前运行的是最新版本 ({
                                update.CurrentlyInstalledVersion?.Version?.ToString() ?? "×"
                            }\{
                                typeof(App).Assembly.GetName().Version.ToString(4)
                            })");
                    }
                    else
                    {
                        if (update.CurrentlyInstalledVersion != null
                            && update.FutureReleaseEntry.Version < update.CurrentlyInstalledVersion.Version)
                        {
                            logger.Warn("服务器回滚了一个更新，本地版本比服务器版本高。");
                        }

                        logger.Info($@"服务器最新版本: {
                            update.FutureReleaseEntry?.Version?.ToString() ?? "×"
                        } 当前本地版本: {
                            update.CurrentlyInstalledVersion?.Version?.ToString() ?? "×"
                        }");

                        logger.Info("开始后台下载新版本（不会影响软件运行）");
                        await manager.DownloadReleases(update.ReleasesToApply);
                        logger.Info("新版本下载完成，开始安装（不会影响软件运行）");
                        await manager.ApplyReleases(update);
                        logger.Info("新版本安装完毕，你可以暂时继续使用当前版本。下次启动时会自动启动最新版本。");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "检查更新时出错，如持续出错请联系开发者 rec@danmuji.org");
            }

            _ = Task.Run(async () => { await Task.Delay(TimeSpan.FromDays(1)); await RunCheckUpdate(); });
        }

        private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            e.Cancel = true;
            (Current.MainWindow as NewMainWindow).CloseWithoutConfirmAction();
        }
    }
}
