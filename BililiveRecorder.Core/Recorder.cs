using BililiveRecorder.Core.Config;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    public class Recorder : IRecorder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private ObservableCollection<IRecordedRoom> Rooms { get; } = new ObservableCollection<IRecordedRoom>();
        public ConfigV1 Config { get; }

        ConfigV1 IRecorder.Config => Config;

        public int Count => Rooms.Count;

        public bool IsReadOnly => true;

        int ICollection<IRecordedRoom>.Count => Rooms.Count;

        bool ICollection<IRecordedRoom>.IsReadOnly => true;

        private readonly Func<int, IRecordedRoom> newIRecordedRoom;
        private CancellationTokenSource tokenSource;

        private bool _valid = false;

        public IRecordedRoom this[int index] => Rooms[index];

        public Recorder(ConfigV1 config, Func<int, IRecordedRoom> iRecordedRoom)
        {
            newIRecordedRoom = iRecordedRoom;
            Config = config;

            BililiveAPI.Config = config;

            tokenSource = new CancellationTokenSource();
            Repeat.Interval(TimeSpan.FromSeconds(3), DownloadWatchdog, tokenSource.Token);

            Rooms.CollectionChanged += (sender, e) =>
            {
                logger.Trace($"Rooms.CollectionChanged;{e.Action};" +
                    $"O:{e.OldItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)};" +
                    $"N:{e.NewItems?.Cast<IRecordedRoom>()?.Select(rr => rr.RoomId.ToString())?.Aggregate((current, next) => current + "," + next)}");
            };

            var debouncing = new SemaphoreSlim(1, 1);
            Config.PropertyChanged += async (sender, e) =>
            {
                if (e.PropertyName == nameof(Config.Cookie))
                {
                    if (await debouncing.WaitAsync(0))
                    {
                        try
                        {
                            logger.Trace("设置 Cookie 等待...");
                            await Task.Delay(100);
                            logger.Trace("设置 Cookie 信息...");
                            await BililiveAPI.ApplyCookieSettings(Config.Cookie);
                            logger.Debug("设置 Cookie 成功");
                        }
                        finally
                        {
                            debouncing.Release();
                        }
                    }
                }
            };
        }

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

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add => (Rooms as INotifyPropertyChanged).PropertyChanged += value;
            remove => (Rooms as INotifyPropertyChanged).PropertyChanged -= value;
        }

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add => (Rooms as INotifyCollectionChanged).CollectionChanged += value;
            remove => (Rooms as INotifyCollectionChanged).CollectionChanged -= value;
        }

        bool IRecorder.Initialize(string workdir) => Initialize(workdir);

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
        public void AddRoom(int roomid) => AddRoom(roomid, false);

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
            if (!_valid) { throw new InvalidOperationException("Not Initialized"); }
            rr.Shutdown();
            logger.Debug("RemoveRoom 移除了直播间 {roomid}", rr.RoomId);
            Rooms.Remove(rr);
        }

        public void Shutdown()
        {
            if (!_valid) { throw new InvalidOperationException("Not Initialized"); }
            logger.Debug("Shutdown called.");
            tokenSource.Cancel();

            SaveConfigToFile();

            Rooms.ToList().ForEach(rr =>
            {
                rr.Shutdown();
            });
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

    }
}
