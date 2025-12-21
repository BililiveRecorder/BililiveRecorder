using System.ComponentModel;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    public class AboutModel : INotifyPropertyChanged
    {
#pragma warning disable CS0067 // The event 'Recorder.PropertyChanged' is never used
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067 // The event 'Recorder.PropertyChanged' is never used

        public string InformationalVersion => GitVersionInformation.InformationalVersion;
    }
}
