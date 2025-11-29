using System.Text.RegularExpressions;

namespace BililiveRecorder.Core
{
    public static class RoomIdFromUrl
    {
        public static readonly Regex Regex = new Regex("""^(?:(?:https?:\/\/)?live\.bilibili\.com\/(?:blanc\/|h5\/)?)?(\d+)\/?(?:[#\?].*)?$""", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
    }
}
