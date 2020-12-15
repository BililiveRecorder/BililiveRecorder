using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config;
using NLog;

namespace BililiveRecorder.Core
{
    public class Recorder : IRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Func<int, IRecordedRoom> newIRecordedRoom;
        private readonly CancellationTokenSource tokenSource;

        private bool _valid = false;
        private bool disposedValue;

        private ObservableCollection<IRecordedRoom> Rooms { get; } = new ObservableCollection<IRecordedRoom>();

        public ConfigV1 Config { get; }

        public int Count => Rooms.Count;
        public bool IsReadOnly => true;
        public IRecordedRoom this[int index] => Rooms[index];

        public Recorder(ConfigV1 config, Func<int, IRecordedRoom> iRecordedRoom)
        {
            newIRecordedRoom = iRecordedRoom;
            Config = config;

            tokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(3), DownloadWatchdog, tokenSource.Token);

            Rooms.CollectionChanged += (sender, e) =>
            {
                logger.Trace($"Rooms.CollectionChanged;{e.Action};" +
                    $"O:{e.OldItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)};" +
                    $"N:{e.NewItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)}");
            };
        }

        public bool Initialize(string workdir)
        {
            logger.Debug("Initialize: " + workdir);
            if (ConfigParser.Load(directory: workdir, config: Config))
            {
                _valid = true;
                Config.WorkDirectory = workdir;
                if ((Config.RoomList?.Count ?? 0) > 0)
                {
                    Config.RoomList.ForEach((r) => AddRoom(r.Roomid, r.Enabled));
                }
                ConfigParser.Save(Config.WorkDirectory, Config);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 添加直播间到录播姬
        /// </summary>
        /// <param name="roomid">房间号（支持短号）</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void AddRoom(int roomid) => AddRoom(roomid, true);

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
                if (!_valid) { throw new InvalidOperationException("Not Initialized"); }
                if (roomid <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(roomid), "房间号需要大于0");
                }

                var rr = newIRecordedRoom(roomid);
                if (enabled)
                {
                    Task.Run(() => rr.Start());
                }

                logger.Debug("AddRoom 添加了 {roomid} 直播间 ", rr.RoomId);
                Rooms.Add(rr);
            }
            catch (Exception ex)
            {
                logger.Debug(ex, "AddRoom 添加 {roomid} 直播间错误 ", roomid);
            }
        }

        /// <summary>
        /// 从录播姬移除直播间
        /// </summary>
        /// <param name="rr">直播间</param>
        public void RemoveRoom(IRecordedRoom rr)
        {
            if (rr is null) return;
            if (!_valid) { throw new InvalidOperationException("Not Initialized"); }
            rr.Shutdown();
            logger.Debug("RemoveRoom 移除了直播间 {roomid}", rr.RoomId);
            Rooms.Remove(rr);
        }

        private void Shutdown()
        {
            if (!_valid) { return; }
            logger.Debug("Shutdown called.");
            tokenSource.Cancel();

            SaveConfigToFile();

            Rooms.ToList().ForEach(rr =>
            {
                rr.Shutdown();
            });

            Rooms.Clear();
        }

        public void SaveConfigToFile()
        {
            Config.RoomList = new List<RoomV1>();
            Rooms.ToList().ForEach(rr =>
            {
                Config.RoomList.Add(new RoomV1()
                {
                    Roomid = rr.RoomId,
                    Enabled = rr.IsMonitoring,
                });
            });

            ConfigParser.Save(Config.WorkDirectory, Config);
        }

        private void DownloadWatchdog()
        {
            if (!_valid) { return; }
            try
            {
                Rooms.ToList().ForEach(room =>
                {
                    if (room.IsRecording)
                    {
                        if (DateTime.Now - room.LastUpdateDateTime > TimeSpan.FromMilliseconds(Config.TimingWatchdogTimeout))
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
        bool ICollection<IRecordedRoom>.Contains(IRecordedRoom item) => Rooms.Contains(item);
        void ICollection<IRecordedRoom>.CopyTo(IRecordedRoom[] array, int arrayIndex) => Rooms.CopyTo(array, arrayIndex);
        public IEnumerator<IRecordedRoom> GetEnumerator() => Rooms.GetEnumerator();
        IEnumerator<IRecordedRoom> IEnumerable<IRecordedRoom>.GetEnumerator() => Rooms.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Rooms.GetEnumerator();

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    Shutdown();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
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
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
