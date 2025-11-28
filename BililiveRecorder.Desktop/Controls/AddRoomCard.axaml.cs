using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public partial class AddRoomCard : UserControl
    {
        public event EventHandler<AddRoomRequestedEventArgs>? AddRoomRequested;

        public AddRoomCard()
        {
            this.InitializeComponent();
        }

        private void AddButton_Click(object? sender, RoutedEventArgs e)
        {
            TryAddRoom();
        }

        private void RoomIdTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryAddRoom();
            }
        }

        private void TryAddRoom()
        {
            var text = RoomIdTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text))
                return;

            if (!int.TryParse(text, out var roomId) || roomId <= 0)
            {
                // Invalid room ID
                return;
            }

            var autoRecord = AutoRecordCheckBox.IsChecked ?? true;

            AddRoomRequested?.Invoke(this, new AddRoomRequestedEventArgs(roomId, autoRecord));

            // Clear the text box
            RoomIdTextBox.Text = "";
        }

        public class AddRoomRequestedEventArgs : EventArgs
        {
            public int RoomId { get; }
            public bool AutoRecord { get; }

            public AddRoomRequestedEventArgs(int roomId, bool autoRecord)
            {
                RoomId = roomId;
                AutoRecord = autoRecord;
            }
        }
    }
}
