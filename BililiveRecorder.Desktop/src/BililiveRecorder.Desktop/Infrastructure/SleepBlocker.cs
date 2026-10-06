using System;
using System.Runtime.InteropServices;
using System.Threading;

#nullable enable
namespace BililiveRecorder.Desktop.Infrastructure
{
    /// <summary>
    /// 录制期间阻止系统进入睡眠。
    /// 从 WPF 版 Program.cs 原样移植。
    /// </summary>
    internal static class SleepBlocker
    {
        internal static void Start()
        {
            var t = new Thread(EntryPoint)
            {
                Name = "SystemSleepBlocker",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            t.Start();
        }

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        private static void EntryPoint()
        {
            try
            {
                while (true)
                {
                    try
                    {
                        _ = SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_CONTINUOUS);
                    }
                    catch (Exception) { }
                    Thread.Sleep(millisecondsTimeout: 30 * 1000);
                }
            }
            catch (Exception) { }
        }
    }
}
