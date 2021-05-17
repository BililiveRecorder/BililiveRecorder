using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V2;
using BililiveRecorder.Core.Event;
using BililiveRecorder.Core.SimpleWebhook;
using Serilog;

namespace BililiveRecorder.Core
{
    public class Recorder : IRecorder
    {
        private readonly object lockObject = new object();
        private readonly ObservableCollection<IRoom> roomCollection;
        private readonly IRoomFactory roomFactory;
        private readonly ILogger logger;
        private readonly BasicWebhookV1 basicWebhookV1;
        private readonly BasicWebhookV2 basicWebhookV2;

        private bool disposedValue;

        public Recorder(IRoomFactory roomFactory, ConfigV2 config, ILogger logger)
        {
            this.roomFactory = roomFactory ?? throw new ArgumentNullException(nameof(roomFactory));
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger?.ForContext<Recorder>() ?? throw new ArgumentNullException(nameof(logger));
            this.roomCollection = new ObservableCollection<IRoom>();
            this.Rooms = new ReadOnlyObservableCollection<IRoom>(this.roomCollection);

            this.basicWebhookV1 = new BasicWebhookV1(config);
            this.basicWebhookV2 = new BasicWebhookV2(config.Global);

            {
                logger.Debug("Recorder created with {RoomCount} rooms", config.Rooms.Count);
                for (var i = 0; i < config.Rooms.Count; i++)
                {
                    var item = config.Rooms[i];
                    if (item is not null)
                        this.AddRoom(roomConfig: item, initDelayFactor: i);
                }

                this.SaveConfig();
            }
        }

        public event EventHandler<AggregatedRoomEventArgs<RecordSessionStartedEventArgs>>? RecordSessionStarted;
        public event EventHandler<AggregatedRoomEventArgs<RecordSessionEndedEventArgs>>? RecordSessionEnded;
        public event EventHandler<AggregatedRoomEventArgs<RecordFileOpeningEventArgs>>? RecordFileOpening;
        public event EventHandler<AggregatedRoomEventArgs<RecordFileClosedEventArgs>>? RecordFileClosed;
        public event EventHandler<AggregatedRoomEventArgs<NetworkingStatsEventArgs>>? NetworkingStats;
        public event EventHandler<AggregatedRoomEventArgs<RecordingStatsEventArgs>>? RecordingStats;
        public event PropertyChangedEventHandler? PropertyChanged;

        public ConfigV2 Config { get; }

        public ReadOnlyObservableCollection<IRoom> Rooms { get; }

        public void AddRoom(int roomid) => this.AddRoom(roomid, true);

        public void AddRoom(int roomid, bool enabled)
        {
            lock (this.lockObject)
            {
                this.logger.Debug("AddRoom {RoomId}, AutoRecord: {AutoRecord}", roomid, enabled);
                var roomConfig = new RoomConfig { RoomId = roomid, AutoRecord = enabled };
                this.AddRoom(roomConfig, 0);
                this.SaveConfig();
            }
        }

        private void AddRoom(RoomConfig roomConfig, int initDelayFactor)
        {
            roomConfig.SetParent(this.Config.Global);
            var room = this.roomFactory.CreateRoom(roomConfig, initDelayFactor);
            this.roomCollection.Add(room);
            this.AddEventSubscription(room);
        }

        public void RemoveRoom(IRoom room)
        {
            lock (this.lockObject)
            {
                if (this.roomCollection.Remove(room))
                {
                    this.RemoveEventSubscription(room);
                    this.logger.Debug("RemoveRoom {RoomId}", room.RoomConfig.RoomId);
                    room.Dispose();
                    this.SaveConfig();
                }
            }
        }

        public void SaveConfig()
        {
            this.Config.Rooms = this.Rooms.Select(x => x.RoomConfig).ToList();
            ConfigParser.SaveTo(this.Config.Global.WorkDirectory!, this.Config);
        }

        #region Events

        private void AddEventSubscription(IRoom room)
        {
            room.RecordSessionStarted += this.Room_RecordSessionStarted;
            room.RecordSessionEnded += this.Room_RecordSessionEnded;
            room.RecordFileOpening += this.Room_RecordFileOpening;
            room.RecordFileClosed += this.Room_RecordFileClosed;
            room.NetworkingStats += this.Room_NetworkingStats;
            room.RecordingStats += this.Room_RecordingStats;
            room.PropertyChanged += this.Room_PropertyChanged;
        }

        private void Room_NetworkingStats(object sender, NetworkingStatsEventArgs e)
        {
            var room = (IRoom)sender;
            NetworkingStats?.Invoke(this, new AggregatedRoomEventArgs<NetworkingStatsEventArgs>(room, e));
        }

        private void Room_RecordingStats(object sender, RecordingStatsEventArgs e)
        {
            var room = (IRoom)sender;
            RecordingStats?.Invoke(this, new AggregatedRoomEventArgs<RecordingStatsEventArgs>(room, e));
        }

        private void Room_RecordFileClosed(object sender, RecordFileClosedEventArgs e)
        {
            var room = (IRoom)sender;
            _ = Task.Run(async () => await this.basicWebhookV2.SendFileClosedAsync(e).ConfigureAwait(false));
            _ = Task.Run(async () => await this.basicWebhookV1.SendAsync(new RecordEndData(e)).ConfigureAwait(false));
            RecordFileClosed?.Invoke(this, new AggregatedRoomEventArgs<RecordFileClosedEventArgs>(room, e));
        }

        private void Room_RecordFileOpening(object sender, RecordFileOpeningEventArgs e)
        {
            var room = (IRoom)sender;
            _ = Task.Run(async () => await this.basicWebhookV2.SendFileOpeningAsync(e).ConfigureAwait(false));
            RecordFileOpening?.Invoke(this, new AggregatedRoomEventArgs<RecordFileOpeningEventArgs>(room, e));
        }

        private void Room_RecordSessionStarted(object sender, RecordSessionStartedEventArgs e)
        {
            var room = (IRoom)sender;
            _ = Task.Run(async () => await this.basicWebhookV2.SendSessionStartedAsync(e).ConfigureAwait(false));
            RecordSessionStarted?.Invoke(this, new AggregatedRoomEventArgs<RecordSessionStartedEventArgs>(room, e));
        }

        private void Room_RecordSessionEnded(object sender, RecordSessionEndedEventArgs e)
        {
            var room = (IRoom)sender;
            _ = Task.Run(async () => await this.basicWebhookV2.SendSessionEndedAsync(e).ConfigureAwait(false));
            RecordSessionEnded?.Invoke(this, new AggregatedRoomEventArgs<RecordSessionEndedEventArgs>(room, e));
        }

        private void Room_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // TODO
            // throw new NotImplementedException();
        }

        private void RemoveEventSubscription(IRoom room)
        {
            room.RecordSessionStarted -= this.Room_RecordSessionStarted;
            room.RecordSessionEnded -= this.Room_RecordSessionEnded;
            room.RecordFileOpening -= this.Room_RecordFileOpening;
            room.RecordFileClosed -= this.Room_RecordFileClosed;
            room.RecordingStats -= this.Room_RecordingStats;
            room.PropertyChanged -= this.Room_PropertyChanged;
        }

        #endregion

        #region Dispose

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    this.logger.Debug("Dispose called");
                    this.SaveConfig();
                    foreach (var room in this.roomCollection)
                        room.Dispose();
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

        #endregion
    }
}
