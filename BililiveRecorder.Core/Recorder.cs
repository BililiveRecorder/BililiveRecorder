using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BililiveRecorder.Core
{
    public class Recorder
    {
        public readonly ObservableCollection<RecordedRoom> Rooms = new ObservableCollection<RecordedRoom>();
        public readonly Settings settings = new Settings();

        public Recorder()
        {

        }

    }
}
