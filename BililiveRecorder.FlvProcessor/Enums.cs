namespace BililiveRecorder.FlvProcessor
{
    public enum TagType : int
    {
        AUDIO = 8,
        VIDEO = 9,
        DATA = 18,
    }

    public enum AMFTypes : byte
    {
        /// <summary>
        /// 非标准类型。在 Decode 过程中作为函数参数使用
        /// </summary>
        Any = 0xFF,
        /// <summary>
        /// Double
        /// </summary>
        Number = 0,
        /// <summary>
        /// 8 bit unsigned integer
        /// </summary>
        Boolean = 1,
        /// <summary>
        /// ScriptDataString
        /// </summary>
        String = 2,
        /// <summary>
        /// ScriptDataObject
        /// </summary>
        Object = 3,
        /// <summary>
        /// Not Supported
        /// </summary>
        MovieClip = 4,
        /// <summary>
        /// Nothing
        /// </summary>
        Null = 5,
        /// <summary>
        /// Nothing
        /// </summary>
        Undefined = 6,
        /// <summary>
        /// Not Supported
        /// </summary>
        Reference = 7,
        /// <summary>
        /// ScriptDataEcmaArray
        /// </summary>
        ECMAArray = 8,
        /// <summary>
        /// Nothing
        /// </summary>
        ObjectEndMarker = 9,
        /// <summary>
        /// ScriptDataStrictArray
        /// </summary>
        StrictArray = 10,
        /// <summary>
        /// ScriptDataDate
        /// </summary>
        Date = 11,
        /// <summary>
        /// ScriptDataLongString
        /// </summary>
        LongString = 12
    }
}
