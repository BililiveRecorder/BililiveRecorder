using BililiveRecorder.Desktop.Infrastructure;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    public class LogViewModel
    {
        public ILocalizationService Loc => LocalizationService.Instance;

        /// <summary>内存日志缓冲（由 MemoryLogSink 写入）。</summary>
        public System.Collections.ObjectModel.ObservableCollection<LogEntry> Logs => MemoryLogSink.Logs;
    }
}
