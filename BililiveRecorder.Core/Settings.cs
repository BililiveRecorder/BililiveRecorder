using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace BililiveRecorder.Core
{
    public class Settings : INotifyPropertyChanged
    {

        public int Clip_Past { get; set; } = 90;
        public int Clip_Future { get; set; } = 30;

        public string SavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);


        public Settings()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
