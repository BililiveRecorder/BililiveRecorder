using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public class CloseWindowConfirmDialog : ContentDialog
    {
        public CloseWindowConfirmDialog()
        {
            Title = "确认关闭 Confirm Close";
            Content = "确定要关闭录播姬吗？\nAre you sure you want to close BililiveRecorder?";
            PrimaryButtonText = "确定 OK";
            CloseButtonText = "取消 Cancel";
            DefaultButton = ContentDialogButton.Close;
        }

        public new async Task<ContentDialogResult> ShowAsync()
        {
            return await base.ShowAsync();
        }
    }
}
