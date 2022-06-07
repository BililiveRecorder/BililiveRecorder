using System.ComponentModel;

namespace BililiveRecorder.Cli.Configure
{
    public enum JsonSchemaSelection
    {
        [Description("https://raw.githubusercontent.com/BililiveRecorder/BililiveRecorder/dev/configV3.schema.json")]
        Default,

        [Description("Custom")]
        Custom
    }
}
