using System;
using System.Threading.Tasks;
using Serilog;
using Squirrel;

#nullable enable
namespace BililiveRecorder.WPF
{
    internal class Update
    {
        private readonly ILogger logger;

        private Task updateInProgress = Task.CompletedTask;

        public Update(ILogger logger)
        {
            this.logger = logger.ForContext<Update>();
        }

        public async Task UpdateAsync()
        {
            if (!this.updateInProgress.IsCompleted)
                await this.updateInProgress;
            this.updateInProgress = this.RealUpdateAsync();
            await this.updateInProgress;
        }

        public async Task WaitForUpdatesOnShutdownAsync() => await this.updateInProgress.ContinueWith(ex => { }, TaskScheduler.Default).ConfigureAwait(false);

        private async Task RealUpdateAsync()
        {
            this.logger.Debug("Checking updates");
            try
            {
                using var updateManager = new UpdateManager(@"https://soft.danmuji.org/BililiveRecorder/");

                var ignoreDeltaUpdates = false;

            retry:
                try
                {
                    var updateInfo = await updateManager.CheckForUpdate(ignoreDeltaUpdates);

                    if (updateInfo.ReleasesToApply.Count == 0)
                    {
                        this.logger.Information("当前运行的是最新版本 {BuildVersion}/{InstalledVersion}",
                            typeof(Update).Assembly.GetName().Version.ToString(4),
                            updateInfo.CurrentlyInstalledVersion?.Version?.ToString() ?? "×");
                    }
                    else
                    {
                        this.logger.Information("有新版本 {RemoteVersion}，当前本地 {BuildVersion}/{InstalledVersion}",
                            updateInfo.FutureReleaseEntry?.Version?.ToString() ?? "×",
                            typeof(Update).Assembly.GetName().Version.ToString(4),
                            updateInfo.CurrentlyInstalledVersion?.Version?.ToString() ?? "×");

                        await updateManager.DownloadReleases(updateInfo.ReleasesToApply);
                        await updateManager.ApplyReleases(updateInfo);

                        this.logger.Information("更新完成");
                    }
                }
                catch (Exception ex)
                {
                    if (ignoreDeltaUpdates == false)
                    {
                        ignoreDeltaUpdates = true;
                        this.logger.Debug(ex, "第一次检查更新出错");
                        goto retry;
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "检查更新时出错");
            }
        }
    }
}
