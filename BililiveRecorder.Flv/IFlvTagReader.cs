using System;
using System.Threading.Tasks;

namespace BililiveRecorder.Flv
{
    /// <summary>
    /// 实现 Flv Tag 的读取
    /// </summary>
    public interface IFlvTagReader : IDisposable
    {
        /// <summary>
        /// Returns the next available Flv Tag but does not consume it.
        /// </summary>
        /// <returns></returns>
        Task<Tag?> PeekTagAsync();

        /// <summary>
        /// Reads the next Flv Tag.
        /// </summary>
        /// <returns></returns>
        Task<Tag?> ReadTagAsync();
    }
}
