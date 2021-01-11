using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using BililiveRecorder.Core.Config.V2;

#nullable enable
namespace BililiveRecorder.Core
{
    public interface IRecorder : INotifyPropertyChanged, INotifyCollectionChanged, IEnumerable<IRecordedRoom>, ICollection<IRecordedRoom>, IDisposable
    {
        ConfigV2? Config { get; }

        bool Initialize(string workdir);

        void AddRoom(int roomid);

        void AddRoom(int roomid, bool enabled);

        void RemoveRoom(IRecordedRoom rr);

        void SaveConfigToFile();

        // void Shutdown();
    }
}
