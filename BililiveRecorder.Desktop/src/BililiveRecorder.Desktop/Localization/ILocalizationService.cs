using System.ComponentModel;
using System.Globalization;

#nullable enable
namespace BililiveRecorder.Desktop.Localization
{
    public interface ILocalizationService : INotifyPropertyChanged
    {
        /// <summary>获取本地化字符串，找不到时返回 key 本身。</summary>
        string this[string key] { get; }

        CultureInfo CurrentCulture { get; set; }
    }
}
