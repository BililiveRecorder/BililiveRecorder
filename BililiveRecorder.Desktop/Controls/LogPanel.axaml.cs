using System.Collections.ObjectModel;
using Avalonia.Controls;
using BililiveRecorder.Desktop.Models;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public partial class LogPanel : UserControl
    {
        public LogPanel()
        {
            this.InitializeComponent();
            this.LogDataGrid.ItemsSource = LogModel.Instance.Logs;
        }
    }
}
