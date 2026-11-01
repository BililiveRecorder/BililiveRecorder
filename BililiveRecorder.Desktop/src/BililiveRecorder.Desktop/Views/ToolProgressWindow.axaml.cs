using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

#nullable enable
namespace BililiveRecorder.Desktop.Views
{
    /// <summary>工具操作进度窗口（带取消按钮）。</summary>
    public partial class ToolProgressWindow : Window
    {
        private readonly CancellationTokenSourceWrapper tokenSource = new CancellationTokenSourceWrapper();

        public ToolProgressWindow()
        {
            this.InitializeComponent();
            this.CancelButton.Content = "取消 Cancel";
        }

        public System.Threading.CancellationToken Token => this.tokenSource.Token;

        public void Report(double percent) => Dispatcher.UIThread.Post(() =>
        {
            this.ProgressBar.Value = Math.Clamp(percent, 0, 100);
            this.ProgressText.Text = $"{(int)Math.Clamp(percent, 0, 100)}%";
        });

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            this.tokenSource.Cancel();
            this.CancelButton.IsEnabled = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            this.tokenSource.Cancel();
            base.OnClosed(e);
        }

        /// <summary>避免直接引用 CancellationTokenSource 时窗口关闭后无法取消的问题。</summary>
        private class CancellationTokenSourceWrapper
        {
            private readonly System.Threading.CancellationTokenSource source = new System.Threading.CancellationTokenSource();

            public System.Threading.CancellationToken Token => this.source.Token;

            public void Cancel() => this.source.Cancel();
        }
    }
}
