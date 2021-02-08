using System.IO;
using JsonSubTypes;
using Newtonsoft.Json;

namespace BililiveRecorder.Flv.Amf
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [JsonConverter(typeof(JsonSubtypes), nameof(Type))]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataNumber), ScriptDataType.Number)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataBoolean), ScriptDataType.Boolean)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataString), ScriptDataType.String)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataObject), ScriptDataType.Object)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataNull), ScriptDataType.Null)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataUndefined), ScriptDataType.Undefined)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataReference), ScriptDataType.Reference)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataEcmaArray), ScriptDataType.EcmaArray)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataStrictArray), ScriptDataType.StrictArray)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataDate), ScriptDataType.Date)]
    [JsonSubtypes.KnownSubType(typeof(ScriptDataLongString), ScriptDataType.LongString)]
    public interface IScriptDataValue
    {
        [JsonProperty]
        ScriptDataType Type { get; }
        void WriteTo(Stream stream);
    }
}
