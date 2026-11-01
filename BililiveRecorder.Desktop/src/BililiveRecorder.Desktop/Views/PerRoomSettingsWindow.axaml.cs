using System;
using Avalonia.Controls;
using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;
using BililiveRecorder.Desktop.ViewModels;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    /// <summary>
    /// 单房间设置窗口，绑定到 IRoom.RoomConfig（与 WPF 版 PerRoomSettingsDialog 相同的字段）。
    /// 关闭窗口即生效（内存中修改），保存时机与 WPF 版一致（SaveConfig 时持久化）。
    /// </summary>
    public partial class PerRoomSettingsWindow : Window
    {
        public PerRoomSettingsWindow()
        {
            this.InitializeComponent();
        }

        public PerRoomSettingsWindow(IRoom room)
            : this()
        {
            this.DataContext = new RoomSettingsViewModel(room, LocalizationService.Instance);
            this.Title = $"{room.Name} ({room.RoomConfig.RoomId})";
        }
    }
}
