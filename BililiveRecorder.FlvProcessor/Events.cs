using System;
using System.Collections.Generic;
using System.Text;

namespace BililiveRecorder.FlvProcessor
{
    public delegate void BlockProcessedEvent(object sender, BlockProcessedArgs e);
    public class BlockProcessedArgs
    {
        public FlvDataBlock DataBlock;
    }


    public delegate void ClipFinalizedEvent(object sender, ClipFinalizedArgs e);
    public class ClipFinalizedArgs
    {
        public FlvClipProcessor ClipProcessor;
    }

}
