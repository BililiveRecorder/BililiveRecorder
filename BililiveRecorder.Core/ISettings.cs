using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public interface ISettings : INotifyPropertyChanged
    {
        uint Clip_Past { get; set; }
        uint Clip_Future { get; set; }
        string SavePath { get; set; }
        EnabledFeature Feature { get; set; }
    }
}
