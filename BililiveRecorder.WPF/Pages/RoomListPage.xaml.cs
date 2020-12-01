using System;
using System.Text.RegularExpressions;
using System.Linq;
using BililiveRecorder.Core;
using BililiveRecorder.WPF.Controls;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for RoomList.xaml
    /// </summary>
    public partial class RoomListPage
    {
        private static readonly Regex RoomIdRegex
            = new Regex(@"^(?:https?:\/\/)?live\.bilibili\.com\/(?:blanc\/|h5\/)?(\d*)(?:\?.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public RoomListPage()
        {
            InitializeComponent();
        }

        private async void RoomCard_DeleteRequested(object sender, EventArgs e)
        {
            if (DataContext is IRecorder rec && sender is IRecordedRoom room)
            {
                var dialog = new DeleteRoomConfirmDialog
                {
                    DataContext = room
                };

                var result = await dialog.ShowAsync();

                if (result == ModernWpf.Controls.ContentDialogResult.Primary)
                {
                    rec.RemoveRoom(room);
                }
            }
        }

        private async void AddRoomCard_AddRoomRequested(object sender, string e)
        {
            var input = e.Trim();
            if (string.IsNullOrWhiteSpace(input) || !(DataContext is IRecorder rec)) return;

            if (!int.TryParse(input, out var roomid))
            {
                var m = RoomIdRegex.Match(input);
                if (m.Success && m.Groups.Count > 1 && int.TryParse(m.Groups[1].Value, out var result2))
                {
                    roomid = result2;
                }
                else
                {
                    await new AddRoomFailedDialog { DataContext = "请输入B站直播房间号或直播间链接" }.ShowAsync();
                    return;
                }
            }

            if (roomid < 0)
            {
                await new AddRoomFailedDialog { DataContext = "房间号不能是负数" }.ShowAsync();
                return;
            }
            else if (roomid == 0)
            {
                await new AddRoomFailedDialog { DataContext = "房间号不能是 0" }.ShowAsync();
                return;
            }

            if (rec.Any(x => x.RoomId == roomid || x.ShortRoomId == roomid))
            {
                await new AddRoomFailedDialog { DataContext = "这个直播间已经被添加过了" }.ShowAsync();
                return;
            }

            rec.AddRoom(roomid);
        }
    }
}
