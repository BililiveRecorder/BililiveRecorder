using System;
using System.ComponentModel;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V2;

#nullable enable
namespace BililiveRecorder.WPF.MockData
{
#if false && DEBUG
    internal class MockRecordedRoom : IRecordedRoom
    {
        private bool disposedValue;

        public MockRecordedRoom()
        {
            this.RoomId = 123456789;
            this.ShortRoomId = 1234;
            this.StreamerName = "Mock主播名Mock主播名Mock主播名Mock主播名";
            this.IsMonitoring = false;
            this.IsRecording = true;
            this.IsStreaming = true;
            this.DownloadSpeedPersentage = 100d;
            this.DownloadSpeedMegaBitps = 2.45d;
        }

        public int ShortRoomId { get; set; }

        public int RoomId { get; set; }

        public string StreamerName { get; set; }

        public string Title { get; set; } = string.Empty;

        public IStreamMonitor StreamMonitor { get; set; } = null!;

        public IFlvStreamProcessor? Processor { get; set; }

        public bool IsMonitoring { get; set; }

        public bool IsRecording { get; set; }

        public bool IsDanmakuConnected { get; set; }

        public bool IsStreaming { get; set; }

        public double DownloadSpeedPersentage { get; set; }

        public double DownloadSpeedMegaBitps { get; set; }

        public DateTime LastUpdateDateTime { get; set; }

        public Guid Guid { get; } = Guid.NewGuid();

        public RoomConfig RoomConfig => new RoomConfig();

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<RecordEndData>? RecordEnded;

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
            this.IsMonitoring = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsMonitoring)));
            return true;
        }

        public void StartRecord()
        {
            this.IsRecording = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsRecording)));
        }

        public void Stop()
        {
            this.IsMonitoring = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsMonitoring)));
        }

        public void StopRecord()
        {
            this.IsRecording = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsRecording)));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
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
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
#endif
}
