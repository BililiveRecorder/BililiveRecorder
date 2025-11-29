using System;

namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class StatusChangedEventArgs : EventArgs
    {
        public static readonly StatusChangedEventArgs True = new StatusChangedEventArgs
        {
            Connected = true
        };
        public static readonly StatusChangedEventArgs False = new StatusChangedEventArgs
        {
            Connected = false
        };

        public bool Connected { get; set; }
    }
}
