using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.Core
{
    public delegate void StreamStatusChangedEvent(object sender, StreamStatusChangedArgs e);
    public class StreamStatusChangedArgs
    {
        public TriggerType type;
    }
}
