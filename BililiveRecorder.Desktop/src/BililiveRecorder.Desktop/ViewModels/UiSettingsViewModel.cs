using System;
using Avalonia.Threading;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>
    /// 界面级开关，直接写入全局配置（WpfShowTitleAndArea / WpfNotifyStreamStart）。
    /// </summary>
    public partial class UiSettingsViewModel : ObservableObject
    {
        private readonly IRecorder recorder;

        public UiSettingsViewModel(IRecorder recorder, ILocalizationService loc)
        {
            this.recorder = recorder;
        }

        /// <summary>房间卡片上是否显示标题与分区（对应 WPF 版"显示标题与分区"菜单）。</summary>
        public bool ShowTitleAndArea
        {
            get => this.recorder.Config.Global.WpfShowTitleAndArea;
            set
            {
                if (this.recorder.Config.Global.WpfShowTitleAndArea == value)
                    return;
                this.recorder.Config.Global.WpfShowTitleAndArea = value;
                this.OnPropertyChanged(nameof(this.ShowTitleAndArea));
            }
        }

        /// <summary>开播时弹出提醒。</summary>
        public bool NotifyStreamStart
        {
            get => this.recorder.Config.Global.WpfNotifyStreamStart;
            set
            {
                if (this.recorder.Config.Global.WpfNotifyStreamStart == value)
                    return;
                this.recorder.Config.Global.WpfNotifyStreamStart = value;
                this.OnPropertyChanged(nameof(this.NotifyStreamStart));
            }
        }

        [RelayCommand]
        private void ToggleShowTitleAndArea() => this.ShowTitleAndArea = !this.ShowTitleAndArea;

        [RelayCommand]
        private void ToggleNotifyStreamStart() => this.NotifyStreamStart = !this.NotifyStreamStart;

        internal void Refresh() => Dispatcher.UIThread.Post(() =>
        {
            this.OnPropertyChanged(nameof(this.ShowTitleAndArea));
            this.OnPropertyChanged(nameof(this.NotifyStreamStart));
        });
    }
}
