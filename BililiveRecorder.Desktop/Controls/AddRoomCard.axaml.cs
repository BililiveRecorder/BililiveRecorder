using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public partial class AddRoomCard : UserControl
    {
        public event EventHandler<string>? AddRoomRequested;

        public AddRoomCard()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object? sender, RoutedEventArgs e)
        {
            var roomId = this.InputTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(roomId))
            {
                AddRoomRequested?.Invoke(this, roomId);
                this.InputTextBox.Text = string.Empty;
            }
        }

        private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(sender, e);
            }
        }
    }
}
