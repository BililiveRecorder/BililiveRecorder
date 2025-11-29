using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#nullable enable
namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for AddRoomCard.xaml
    /// </summary>
    public partial class AddRoomCard : UserControl
    {
        public event EventHandler<string>? AddRoomRequested;

        public AddRoomCard()
        {
            this.InitializeComponent();
        }

        private void AddRoom()
        {
            AddRoomRequested?.Invoke(this, this.InputTextBox.Text);
            this.InputTextBox.Text = string.Empty;
            this.InputTextBox.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.AddRoom();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.AddRoom();
            }
        }
    }
}
