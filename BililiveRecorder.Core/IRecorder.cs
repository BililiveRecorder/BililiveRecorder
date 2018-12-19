using BililiveRecorder.Core.Config;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public interface IRecorder : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<IRecordedRoom>, ICollection<IRecordedRoom>
    {
        ConfigV1 Config { get; }

        bool Initialize(string workdir);

        void AddRoom(int roomid);

        void AddRoom(int roomid, bool enabled);

        void RemoveRoom(IRecordedRoom rr);

        void Shutdown();
    }
}
