namespace BililiveRecorder.WPF.Controls
{
    /// <summary>
    /// Interaction logic for AddRoomFailedDialog.xaml
    /// </summary>
    public partial class AddRoomFailedDialog
    {
        public AddRoomFailedDialog()
        {
            this.InitializeComponent();
        }

        public enum AddRoomFailedErrorText
        {
            InvalidInput,
            Duplicate,
            RoomIdZero,
            RoomIdNegative,
        }
    }
}
