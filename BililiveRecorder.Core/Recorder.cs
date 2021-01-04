using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using BililiveRecorder.Core.Callback;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V2;
using NLog;

#nullable enable
namespace BililiveRecorder.Core
{
    public class Recorder : IRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Func<RoomConfig, IRecordedRoom> newIRecordedRoom;
        private readonly CancellationTokenSource tokenSource;

        private bool _valid = false;
        private bool disposedValue;

        private ObservableCollection<IRecordedRoom> Rooms { get; } = new ObservableCollection<IRecordedRoom>();

        public ConfigV2? Config { get; private set; }

        private BasicWebhook? Webhook { get; set; }

        public int Count => this.Rooms.Count;
        public bool IsReadOnly => true;
        public IRecordedRoom this[int index] => this.Rooms[index];

        public Recorder(Func<RoomConfig, IRecordedRoom> iRecordedRoom)
        {
            this.newIRecordedRoom = iRecordedRoom ?? throw new ArgumentNullException(nameof(iRecordedRoom));

            this.tokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(3), this.DownloadWatchdog, this.tokenSource.Token);

            this.Rooms.CollectionChanged += (sender, e) =>
            {
                logger.Trace($"Rooms.CollectionChanged;{e.Action};" +
                    $"O:{e.OldItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)};" +
                    $"N:{e.NewItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)}");
            };
        }

        public bool Initialize(string workdir)
        {
            if (this._valid)
                throw new InvalidOperationException("Recorder is in valid state");
            logger.Debug("Initialize: " + workdir);
            var config = ConfigParser.LoadFrom(directory: workdir);
            if (config is not null)
            {
                this.Config = config;
                this.Config.Global.WorkDirectory = workdir;
                this.Webhook = new BasicWebhook(this.Config);
                this._valid = true;
                this.Config.Rooms.ForEach(r => this.AddRoom(r));
                ConfigParser.SaveTo(this.Config.Global.WorkDirectory, this.Config);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool InitializeWithConfig(ConfigV2 config)
        {
            // 脏写法 but it works
            if (this._valid)
                throw new InvalidOperationException("Recorder is in valid state");

            if (config is null)
                throw new ArgumentNullException(nameof(config));

            logger.Debug("Initialize With Config.");
            this.Config = config;
            this.Webhook = new BasicWebhook(this.Config);
            this._valid = true;
            this.Config.Rooms.ForEach(r => this.AddRoom(r));
            return true;
        }

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomid">房间号（支持短号）</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void AddRoom(int roomid) => this.AddRoom(roomid, true);

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomid">房间号（支持短号）</param>
        /// <param name="enabled">是否默认启用</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void AddRoom(int roomid, bool enabled)
        {
            try
            {
                if (!this._valid) { throw new InvalidOperationException("Not Initialized"); }
                if (roomid <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(roomid), "房间号需要大于0");
                }

                var config = new RoomConfig
                {
                    RoomId = roomid,
                    AutoRecord = enabled,
                };

                this.AddRoom(config);
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "AddRoom 添加 {roomid} 直播间错误 ", roomid);
            }
        }

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomConfig">房间设置</param>
        public void AddRoom(RoomConfig roomConfig)
        {
            try
            {
                if (!this._valid) { throw new InvalidOperationException("Not Initialized"); }

                roomConfig.SetParent(this.Config?.Global);
                var rr = this.newIRecordedRoom(roomConfig);

                logger.Debug("AddRoom 添加了 {roomid} 直播间 ", rr.RoomId);
                rr.RecordEnded += this.RecordedRoom_RecordEnded;
                this.Rooms.Add(rr);
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "AddRoom 添加 {roomid} 直播间错误 ", roomConfig.RoomId);
            }
        }

        /// <summary>
        /// 从录播姬移除直播间
        /// </summary>
        /// <param name="rr">直播间</param>
        public void RemoveRoom(IRecordedRoom rr)
        {
            if (rr is null) return;
            if (!this._valid) { throw new InvalidOperationException("Not Initialized"); }
            rr.Shutdown();
            rr.RecordEnded -= this.RecordedRoom_RecordEnded;
            logger.Debug("RemoveRoom 移除了直播间 {roomid}", rr.RoomId);
            this.Rooms.Remove(rr);
        }

        private void Shutdown()
        {
            if (!this._valid) { return; }
            logger.Debug("Shutdown called.");
            this.tokenSource.Cancel();

            this.SaveConfigToFile();

            this.Rooms.ToList().ForEach(rr =>
            {
                rr.Shutdown();
            });

            this.Rooms.Clear();
        }

        private void RecordedRoom_RecordEnded(object sender, RecordEndData e) => this.Webhook?.Send(e);

        public void SaveConfigToFile()
        {
            if (this.Config is null) return;

            this.Config.Rooms = this.Rooms.Select(x => x.RoomConfig).ToList();
            ConfigParser.SaveTo(this.Config.Global.WorkDirectory!, this.Config);
        }

        private void DownloadWatchdog()
        {
            if (!this._valid) { return; }
            try
            {
                this.Rooms.ToList().ForEach(room =>
                {
                    if (room.IsRecording)
                    {
                        if (DateTime.Now - room.LastUpdateDateTime > TimeSpan.FromMilliseconds(this.Config!.Global.TimingWatchdogTimeout))
                        {
                            logger.Warn("服务器未断开连接但停止提供 [{roomid}] 直播间的直播数据，通常是录制侧网络不稳定导致，将会断开重连", room.RoomId);
                            room.StopRecord();
                            room.StartRecord();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "直播流下载监控出错");
            }
        }

        void ICollection<IRecordedRoom>.Add(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");
        void ICollection<IRecordedRoom>.Clear() => throw new NotSupportedException("Collection is readonly");
        bool ICollection<IRecordedRoom>.Remove(IRecordedRoom item) => throw new NotSupportedException("Collection is readonly");
        bool ICollection<IRecordedRoom>.Contains(IRecordedRoom item) => this.Rooms.Contains(item);
        void ICollection<IRecordedRoom>.CopyTo(IRecordedRoom[] array, int arrayIndex) => this.Rooms.CopyTo(array, arrayIndex);
        public IEnumerator<IRecordedRoom> GetEnumerator() => this.Rooms.GetEnumerator();
        IEnumerator<IRecordedRoom> IEnumerable<IRecordedRoom>.GetEnumerator() => this.Rooms.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.Rooms.GetEnumerator();

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

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.Shutdown();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                this.disposedValue = true;
            }
        }

        // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Recorder()
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
}
