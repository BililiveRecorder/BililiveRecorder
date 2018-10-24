namespace BililiveRecorder.Core
{
    public interface IStreamMonitor
    {
        int Roomid { get; }
        event StreamStatusChangedEvent StreamStatusChanged;
        DanmakuReceiver receiver { get; } // TODO: 改掉这个写法
        bool Start();
        void Stop();
        void Check(TriggerType type = TriggerType.HttpApiRecheck);
        void CheckAfterSeconeds(int seconds, TriggerType type = TriggerType.HttpApiRecheck);
    }
}
