using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.Core
{
    public class RecordedRoom : INotifyPropertyChanged
    {
        public int RoomID;

        public FlvStreamProcessor Processor; // FlvProcessor

        public readonly ObservableCollection<object> Clips = new ObservableCollection<object>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
