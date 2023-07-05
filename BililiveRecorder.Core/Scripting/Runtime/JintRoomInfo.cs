using System;
using Jint;
using Jint.Native.Json;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintRoomInfo : ObjectInstance
    {
        public JintRoomInfo(Engine engine, IRoom room) : base(engine)
        {
            if (room is null) throw new ArgumentNullException(nameof(room));

            this.FastSetProperty("roomId", new PropertyDescriptor(room.RoomConfig.RoomId, false, true, false));
            this.FastSetProperty("shortId", new PropertyDescriptor(room.ShortId, false, true, false));
            this.FastSetProperty("name", new PropertyDescriptor(room.Name, false, true, false));
            this.FastSetProperty("title", new PropertyDescriptor(room.Title, false, true, false));
            this.FastSetProperty("areaParent", new PropertyDescriptor(room.AreaNameParent, false, true, false));
            this.FastSetProperty("areaChild", new PropertyDescriptor(room.AreaNameChild, false, true, false));
            this.FastSetProperty("objectId", new PropertyDescriptor(room.ObjectId.ToString(), false, true, false));

            var apiData = new JsonParser(engine).Parse(room.RawBilibiliApiJsonData?.ToString() ?? "{}");
            this.FastSetProperty("apiData", new PropertyDescriptor(apiData, false, true, false));
        }
    }
}
