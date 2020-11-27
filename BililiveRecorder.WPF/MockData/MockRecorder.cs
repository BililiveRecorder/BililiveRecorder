using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;

namespace BililiveRecorder.WPF.MockData
{
#if DEBUG
    internal class MockRecorder : IRecorder
    {
        private bool disposedValue;

        public MockRecorder()
        {
            Rooms.Add(new MockRecordedRoom
            {
                IsMonitoring = false,
                IsRecording = false
            });
            Rooms.Add(new MockRecordedRoom
            {
                IsMonitoring = true,
                IsRecording = false
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 100,
                DownloadSpeedMegaBitps = 12.45
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 95,
                DownloadSpeedMegaBitps = 789.45
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 90
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 85
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 80
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 75
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 70
            });
            Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 109
            });

        }

        private ObservableCollection<IRecordedRoom> Rooms { get; } = new ObservableCollection<IRecordedRoom>();

        public ConfigV1 Config { get; } = new ConfigV1();

        public int Count => Rooms.Count;

        public bool IsReadOnly => true;

        int ICollection<IRecordedRoom>.Count => Rooms.Count;

        bool ICollection<IRecordedRoom>.IsReadOnly => true;

        public IRecordedRoom this[int index] => Rooms[index];

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => (Rooms as INotifyPropertyChanged).PropertyChanged += value;
            remove => (Rooms as INotifyPropertyChanged).PropertyChanged -= value;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => (Rooms as INotifyCollectionChanged).CollectionChanged += value;
            remove => (Rooms as INotifyCollectionChanged).CollectionChanged -= value;
        }

        void ICollection<IRecordedRoom>.Add(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");

        void ICollection<IRecordedRoom>.Clear() => throw new NotSupportedException("Collection is readonly");

        bool ICollection<IRecordedRoom>.Remove(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");

        bool ICollection<IRecordedRoom>.Contains(IRecordedRoom item) => Rooms.Contains(item);

        void ICollection<IRecordedRoom>.CopyTo(IRecordedRoom[] array, int arrayIndex) => Rooms.CopyTo(array, arrayIndex);

        public IEnumerator<IRecordedRoom> GetEnumerator() => Rooms.GetEnumerator();

        IEnumerator<IRecordedRoom> IEnumerable<IRecordedRoom>.GetEnumerator() => Rooms.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Rooms.GetEnumerator();

        public bool Initialize(string workdir)
        {
            Config.WorkDirectory = workdir;
            return true;
        }

        public void AddRoom(int roomid)
        {
            AddRoom(roomid, false);
        }

        public void AddRoom(int roomid, bool enabled)
        {
            Rooms.Add(new MockRecordedRoom { RoomId = roomid, IsMonitoring = enabled });
        }

        public void RemoveRoom(IRecordedRoom rr)
        {
            Rooms.Remove(rr);
        }

        public void SaveConfigToFile()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Rooms.Clear();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MockRecorder()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
#endif
}
