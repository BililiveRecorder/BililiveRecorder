using System.ComponentModel;

namespace BililiveRecorder.Cli.Configure
{
    public enum JsonSchemaSelection
    {
        [Description("https://raw.githubusercontent.com/.../config.schema.json")]
        Default,

        [Description("Custom")]
        Custom
    }
}
