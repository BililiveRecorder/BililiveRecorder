using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace BililiveRecorder.WPF.Pages
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsPage.xaml
    /// </summary>
    public partial class AdvancedSettingsPage
    {
        public AdvancedSettingsPage()
        {
            this.InitializeComponent();
        }

        private void Crash_Click(object sender, RoutedEventArgs e) => throw new TestException("test crash triggered");

        public class TestException : Exception
        {
            public TestException() { }
            public TestException(string message) : base(message) { }
            public TestException(string message, Exception innerException) : base(message, innerException) { }
            protected TestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        }

        private void Throw_In_Task_Click(object sender, RoutedEventArgs e) => _ = Task.Run(() =>
        {
            throw new TestException("test task exception triggered");
        });
    }
}
