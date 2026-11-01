using BililiveRecorder.Core;
using BililiveRecorder.Desktop.Localization;

#nullable enable
namespace BililiveRecorder.Desktop.ViewModels
{
    /// <summary>单房间设置窗口的数据上下文：房间 + 本地化。</summary>
    public class RoomSettingsViewModel
    {
        public RoomSettingsViewModel(IRoom room, ILocalizationService loc)
        {
            this.Room = room;
            this.Loc = loc;
        }

        public IRoom Room { get; }

        public ILocalizationService Loc { get; }
    }
}
