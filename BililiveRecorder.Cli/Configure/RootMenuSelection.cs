using System.ComponentModel;

namespace BililiveRecorder.Cli.Configure
{
    public enum RootMenuSelection
    {
        [Description("List rooms")]
        ListRooms,

        [Description("Add room")]
        AddRoom,

        [Description("Delete room")]
        DeleteRoom,

        [Description("Update room config")]
        SetRoomConfig,

        [Description("Update global config")]
        SetGlobalConfig,

        [Description("Update JSON Schema")]
        SetJsonSchema,

        [Description("Exit and discard all changes")]
        Exit,

        [Description("Save and Exit")]
        SaveAndExit,
    }
}
