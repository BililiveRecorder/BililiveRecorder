using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

#nullable enable
namespace BililiveRecorder.Desktop.Pages
{
    public partial class AdvancedSettingsPage : UserControl
    {
        public AdvancedSettingsPage()
        {
            this.InitializeComponent();
        }

        private void TestScript_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement script testing
        }

        private void TestCookie_Click(object? sender, RoutedEventArgs e)
        {
            // TODO: Implement cookie testing
        }

        private void Crash_Click(object? sender, RoutedEventArgs e)
        {
            throw new Exception("Manual crash test");
        }
    }
}
