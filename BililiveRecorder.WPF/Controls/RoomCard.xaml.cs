using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BililiveRecorder.Core;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for RoomCard.xaml
    /// </summary>
    public partial class RoomCard : UserControl
    {
        public RoomCard()
        {
            this.InitializeComponent();
        }

        public event EventHandler DeleteRequested;

        public event EventHandler ShowSettingsRequested;

        private void MenuItem_StartRecording_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.StartRecord();

        private void MenuItem_StopRecording_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.StopRecord();

        private void MenuItem_RefreshInfo_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.RefreshRoomInfo();

        private void MenuItem_StartMonitor_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.Start();

        private void MenuItem_StopMonitor_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.Stop();

        private void MenuItem_DeleteRoom_Click(object sender, RoutedEventArgs e) => DeleteRequested?.Invoke(this.DataContext, EventArgs.Empty);

        private void MenuItem_ShowSettings_Click(object sender, RoutedEventArgs e) => ShowSettingsRequested?.Invoke(this.DataContext, EventArgs.Empty);

        private void Button_Clip_Click(object sender, RoutedEventArgs e) => (this.DataContext as IRecordedRoom)?.Clip();

        private void MenuItem_OpenInBrowser_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is IRecordedRoom r && r is not null)
            {
                try
                {
                    Process.Start("https://live.bilibili.com/" + r.RoomId);
                }
                catch (Exception) { }
            }
        }
    }
}
