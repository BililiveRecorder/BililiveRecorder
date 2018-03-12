namespace BililiveRecorder.FlvProcessor
{
    public enum TagType
    {
        NONE = 0x0,
        AUDIO = 0x8,
        VIDEO = 0x9,
        META = 0x12,
    }

    public enum AMFTypes
    {
        Number = 0x00, // (Encoded as IEEE 64-bit double-precision floating point number)
        Boolean = 0x01, // (Encoded as a single byte of value 0x00 or 0x01)
        String = 0x02, //(ASCII encoded)
        Object = 0x03, // (Set of key/value pairs)
        Null = 0x05,
        Array = 0x08,
        End = 0x09,
    }


}
