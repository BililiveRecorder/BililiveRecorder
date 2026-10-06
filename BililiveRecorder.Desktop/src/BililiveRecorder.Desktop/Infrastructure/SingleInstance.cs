using System;
using System.Text;
using System.Threading;

#nullable enable
namespace BililiveRecorder.Desktop.Infrastructure
{
    /// <summary>
    /// 防止在同一个工作目录同时运行多个录制进程。
    /// 与 WPF 版使用相同的互斥体名称，因此两个版本互斥。
    /// （WPF 版的 IPC 激活依赖 System.Runtime.Remoting，.NET 8 上不可用；
    /// 这里改用命名事件信号实现"二次启动唤起已运行实例"。）
    /// </summary>
    internal static class SingleInstance
    {
        private static Mutex? singleInstanceMutex;
        private static EventWaitHandle? activationEvent;

        public static bool CheckMutex(string path)
        {
            var b64path = Convert.ToBase64String(Encoding.UTF8.GetBytes(path)).Replace('+', '_').Replace('/', '-');
            // 注意: "SingeInstance" 的拼写错误保留，以与 WPF 版的互斥体名称保持一致
            var identifier = "BililiveRecorder:SingeInstance:" + b64path;

            singleInstanceMutex = new Mutex(true, identifier, out var createdNew);

            if (!createdNew)
            {
                singleInstanceMutex.Dispose();
                singleInstanceMutex = null;

                // 已有实例在运行：尝试通过命名事件唤起它的窗口（仅新版响应，WPF 版无此事件）
                TryNotifyExisting(b64path);
                return false;
            }

            try
            {
                activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "BililiveRecorder:Activate:" + b64path);
            }
            catch (Exception)
            {
                activationEvent = null;
            }

            return true;
        }

        /// <summary>启动后台线程等待唤起信号，收到后在新实例窗口上回调。</summary>
        public static void StartActivationWatcher(Action onActivate)
        {
            var evt = activationEvent;
            if (evt is null)
                return;

            var thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        evt.WaitOne();
                        onActivate();
                    }
                }
                catch (Exception)
                {
                    // 事件被释放或进程退出，结束线程
                }
            })
            {
                IsBackground = true,
                Name = "SingleInstanceActivationWatcher",
            };
            thread.Start();
        }

        private static void TryNotifyExisting(string b64path)
        {
            try
            {
                // 用构造函数代替 OpenExisting（后者仅 Windows 可用）：
                // 事件已存在时 createdNew 为 false，说明正在运行的实例可被唤起
                using var evt = new EventWaitHandle(false, EventResetMode.AutoReset, "BililiveRecorder:Activate:" + b64path, out var createdNew);
                if (!createdNew)
                    evt.Set();
            }
            catch (Exception)
            {
                // 平台不支持命名事件等异常情况，忽略
            }
        }

        public static void Cleanup()
        {
            try
            {
                singleInstanceMutex?.ReleaseMutex();
            }
            catch (Exception) { }

            singleInstanceMutex?.Dispose();
            singleInstanceMutex = null;

            activationEvent?.Dispose();
            activationEvent = null;
        }
    }
}
