using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for AddRoomCard.xaml
    /// </summary>
    public partial class AddRoomCard : UserControl
    {
        public event EventHandler<string> AddRoomRequested;

        public AddRoomCard()
        {
            InitializeComponent();
        }

        private void AddRoom()
        {
            AddRoomRequested?.Invoke(this, InputTextBox.Text);
            InputTextBox.Text = string.Empty;
            InputTextBox.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddRoom();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddRoom();
            }
        }
    }
}
