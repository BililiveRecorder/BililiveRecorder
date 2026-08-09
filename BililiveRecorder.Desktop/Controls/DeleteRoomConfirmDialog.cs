using System.Threading.Tasks;
using BililiveRecorder.Core;
using FluentAvalonia.UI.Controls;

#nullable enable
namespace BililiveRecorder.Desktop.Controls
{
    public class DeleteRoomConfirmDialog : ContentDialog
    {
        public IRoom? Room { get; set; }

        public DeleteRoomConfirmDialog()
        {
            Title = "删除房间 Delete Room";
            PrimaryButtonText = "删除 Delete";
            CloseButtonText = "取消 Cancel";
            DefaultButton = ContentDialogButton.Close;
        }

        public new async Task<ContentDialogResult> ShowAsync()
        {
            var roomName = Room?.Name ?? "Unknown";
            var roomId = Room?.RoomConfig.RoomId ?? 0;
            Content = $"确定要删除房间 {roomName} ({roomId}) 吗？\nAre you sure you want to delete room {roomName} ({roomId})?";
            return await base.ShowAsync();
        }
    }
}
