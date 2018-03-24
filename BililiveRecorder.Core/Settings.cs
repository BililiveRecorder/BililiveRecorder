using NLog;
using System.Collections.Generic;
using System.ComponentModel;

namespace BililiveRecorder.Core
{
    public class Settings : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private uint _clip_past = 90;
        public uint Clip_Past
        {
            get => _clip_past;
            set => SetField(ref _clip_past, value, nameof(Clip_Past));
        }

        private uint _clip_future = 30;
        public uint Clip_Future
        {
            get => _clip_future;
            set => SetField(ref _clip_future, value, nameof(Clip_Future));
        }

        private string _savepath = string.Empty;
        public string SavePath
        {
            get => _savepath;
            set => SetField(ref _savepath, value, nameof(SavePath));
        }


        public Settings()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            logger.Debug("设置 [{0}] 的值已从 [{1}] 修改到 [{2}]", propertyName, field, value);
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
