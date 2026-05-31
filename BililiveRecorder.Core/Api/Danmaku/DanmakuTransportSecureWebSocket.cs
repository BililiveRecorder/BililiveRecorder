namespace BililiveRecorder.Core.Api.Danmaku
{
    internal class DanmakuTransportSecureWebSocket : DanmakuTransportWebSocket
    {
        public DanmakuTransportSecureWebSocket(string? bindAddress = null) : base(bindAddress) { }

        protected override string Scheme => "wss";
    }
}
