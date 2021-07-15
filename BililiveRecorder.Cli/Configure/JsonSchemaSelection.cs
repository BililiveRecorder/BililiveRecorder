using System.ComponentModel;

namespace BililiveRecorder.Cli.Configure
{
    public enum JsonSchemaSelection
    {
        [Description("https://raw.githubusercontent.com/Bililive/BililiveRecorder/dev-1.3/configV2.schema.json")]
        Default,

        [Description("Custom")]
        Custom
    }
}
