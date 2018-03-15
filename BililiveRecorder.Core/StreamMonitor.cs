using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.Core
{
    public class StreamMonitor
    {

        public event StreamStatusChangedEvent StreamStatusChanged;

        public void Check()
        {
            throw new NotImplementedException();
        }

        public void CheckAfterSeconeds(uint seconds)
        {
            throw new NotImplementedException();
        }
    }
}
