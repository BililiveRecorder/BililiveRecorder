namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuTransportSecureWebSocket : DanmakuTransportWebSocket
    {
        protected override string Scheme => "wss";
    }
}
