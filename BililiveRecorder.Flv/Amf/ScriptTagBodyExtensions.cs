namespace BililiveRecorder.Flv.Amf
{
    public static class ScriptTagBodyExtensions
    {
        public static ScriptDataEcmaArray? GetMetadataValue(this ScriptTagBody body) => body.Values.Count > 1 ? body.Values[1] as ScriptDataEcmaArray : null;
    }
}
