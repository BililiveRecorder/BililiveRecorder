using System;
using System.ComponentModel;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;

namespace BililiveRecorder.WPF.MockData
{
#if DEBUG
    internal class MockRecordedRoom : IRecordedRoom
    {
        private bool disposedValue;

        public MockRecordedRoom()
        {
            RoomId = 123456789;
            ShortRoomId = 1234;
            StreamerName = "Mock主播名Mock主播名Mock主播名Mock主播名";
            IsMonitoring = false;
            IsRecording = true;
            DownloadSpeedPersentage = 100d;
            DownloadSpeedMegaBitps = 2.45d;
        }

        public int ShortRoomId { get; set; }

        public int RoomId { get; set; }

        public string StreamerName { get; set; }

        public IStreamMonitor StreamMonitor { get; set; }

        public IFlvStreamProcessor Processor { get; set; }

        public bool IsMonitoring { get; set; }

        public bool IsRecording { get; set; }

        public double DownloadSpeedPersentage { get; set; }

        public double DownloadSpeedMegaBitps { get; set; }

        public DateTime LastUpdateDateTime { get; set; }

        public Guid Guid { get; } = Guid.NewGuid();

        public event PropertyChangedEventHandler PropertyChanged;

        public void Clip()
        {
        }

        public void RefreshRoomInfo()
        {
        }

        public void Shutdown()
        {
        }

        public bool Start()
        {
            IsMonitoring = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMonitoring)));
            return true;
        }

        public void StartRecord()
        {
            IsRecording = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording)));
        }

        public void Stop()
        {
            IsMonitoring = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMonitoring)));
        }

        public void StopRecord()
        {
            IsRecording = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording)));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MockRecordedRoom()
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
