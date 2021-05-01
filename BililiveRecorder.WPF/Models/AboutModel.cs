using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace BililiveRecorder.WPF.Models
{
    public class AboutModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string InformationalVersion => GitVersionInformation.InformationalVersion;
    }
}
