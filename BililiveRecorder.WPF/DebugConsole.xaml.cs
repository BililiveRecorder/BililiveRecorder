using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BililiveRecorder.WPF
{
    /// <summary>
    /// Interaction logic for DebugConsole.xaml
    /// </summary>
    public partial class DebugConsole : Window
    {
        private MainWindow mw;
        public DebugConsole(MainWindow _mw)
        {
            mw = _mw;
            InitializeComponent();
        }

        private void ShutdownRecord_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => mw.Recorder.Rooms.ToList().ForEach((rr) => rr.StopRecord()));
        }
    }
}
