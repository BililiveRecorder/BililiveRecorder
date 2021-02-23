using System.Collections.ObjectModel;

namespace BililiveRecorder.WPF.Models
{
    internal class LogModel : ReadOnlyObservableCollection<WpfLogEventSink.LogModel>
    {
        public LogModel() : base(WpfLogEventSink.Logs)
        {
        }
    }
}
