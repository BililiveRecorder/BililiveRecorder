namespace BililiveRecorder.Flv.Amf
{
    public static class ScriptTagBodyExtensions
    {
        public static ScriptDataEcmaArray? GetMetadataValue(this ScriptTagBody body)
        {
            if (body.Values.Count > 1)
            {
                return body.Values[1] switch
                {
                    ScriptDataEcmaArray array => array,
                    ScriptDataObject oect => oect,
                    _ => null
                };
            }
            else return null;
        }
    }
}
