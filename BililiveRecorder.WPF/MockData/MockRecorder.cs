using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;

namespace BililiveRecorder.WPF.MockData
{
#if false && DEBUG
    internal class MockRecorder : IRecorder
    {
        private bool disposedValue;

        public MockRecorder()
        {
            this.Rooms.Add(new MockRecordedRoom
            {
                IsMonitoring = false,
                IsRecording = false
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                IsMonitoring = true,
                IsRecording = false
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 100,
                DownloadSpeedMegaBitps = 12.45
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 95,
                DownloadSpeedMegaBitps = 789.45
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 90
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 85
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 80
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 75
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 70
            });
            this.Rooms.Add(new MockRecordedRoom
            {
                DownloadSpeedPersentage = 109
            });
        }

        private ObservableCollection<IRecordedRoom> Rooms { get; } = new ObservableCollection<IRecordedRoom>();

        public ConfigV2 Config { get; } = new ConfigV2();

        public int Count => this.Rooms.Count;

        public bool IsReadOnly => true;

        int ICollection<IRecordedRoom>.Count => this.Rooms.Count;

        bool ICollection<IRecordedRoom>.IsReadOnly => true;

        public IRecordedRoom this[int index] => this.Rooms[index];

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => (this.Rooms as INotifyPropertyChanged).PropertyChanged += value;
            remove => (this.Rooms as INotifyPropertyChanged).PropertyChanged -= value;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => (this.Rooms as INotifyCollectionChanged).CollectionChanged += value;
            remove => (this.Rooms as INotifyCollectionChanged).CollectionChanged -= value;
        }

        void ICollection<IRecordedRoom>.Add(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");

        void ICollection<IRecordedRoom>.Clear() => throw new NotSupportedException("Collection is readonly");

        bool ICollection<IRecordedRoom>.Remove(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");

        bool ICollection<IRecordedRoom>.Contains(IRecordedRoom item) => this.Rooms.Contains(item);

        void ICollection<IRecordedRoom>.CopyTo(IRecordedRoom[] array, int arrayIndex) => this.Rooms.CopyTo(array, arrayIndex);

        public IEnumerator<IRecordedRoom> GetEnumerator() => this.Rooms.GetEnumerator();

        IEnumerator<IRecordedRoom> IEnumerable<IRecordedRoom>.GetEnumerator() => this.Rooms.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.Rooms.GetEnumerator();

        public bool Initialize(string workdir)
        {
            this.Config.Global.WorkDirectory = workdir;
            return true;
        }

        public void AddRoom(int roomid)
        {
            this.AddRoom(roomid, false);
        }

        public void AddRoom(int roomid, bool enabled)
        {
            this.Rooms.Add(new MockRecordedRoom { RoomId = roomid, IsMonitoring = enabled });
        }

        public void RemoveRoom(IRecordedRoom rr)
        {
            this.Rooms.Remove(rr);
        }

        public void SaveConfigToFile()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.Rooms.Clear();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
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
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
#endif
}
