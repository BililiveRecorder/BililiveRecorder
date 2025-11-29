using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BililiveRecorder.Flv.Amf
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ScriptDataType : byte
    {
        Number = 0,
        Boolean = 1,
        String = 2,
        Object = 3,
        MovieClip = 4,
        Null = 5,
        Undefined = 6,
        Reference = 7,
        EcmaArray = 8,
        ObjectEndMarker = 9,
        StrictArray = 10,
        Date = 11,
        LongString = 12,
    }
}
