using System;
using System.Linq;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public IRecorder Recorder { get; }

        public ILocalizationService Loc { get; }

        public RoomListViewModel RoomList { get; }

        public SettingsViewModel SettingsVm { get; }

        public LogViewModel LogVm { get; }

        /// <summary>窗口标题："mikufans录播姬 (录制中数量)"，随录制状态变化。</summary>
        public string Title => string.Format(this.Loc["Window_Title"], this.Recorder.Rooms.Count(x => x.Recording));

        public MainWindowViewModel(IRecorder recorder, ILocalizationService loc)
        {
            this.Recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            this.Loc = loc ?? throw new ArgumentNullException(nameof(loc));
            this.RoomList = new RoomListViewModel(recorder, loc);
            this.SettingsVm = new SettingsViewModel(recorder.Config.Global, loc);
            this.LogVm = new LogViewModel();

            recorder.RecordSessionStarted += (s, e) => this.NotifyTitleChanged();
            recorder.RecordSessionEnded += (s, e) => this.NotifyTitleChanged();
        }

        private void NotifyTitleChanged()
        {
            if (Dispatcher.UIThread.CheckAccess())
                this.OnPropertyChanged(nameof(this.Title));
            else
                Dispatcher.UIThread.Post(() => this.OnPropertyChanged(nameof(this.Title)));
        }
    }
}
