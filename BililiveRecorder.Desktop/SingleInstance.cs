using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Serilog;

#nullable enable
namespace BililiveRecorder.Desktop
{
    /// <summary>
    /// Cross-platform single instance implementation using named pipes
    /// </summary>
    public static class SingleInstance
    {
        private static readonly ILogger logger = Log.ForContext(typeof(SingleInstance));
        private static Mutex? mutex;
        private static CancellationTokenSource? cts;
        private static Thread? listenerThread;

        public static event EventHandler? NotificationReceived;

        public static bool CheckMutex(string path)
        {
            var mutexName = GetMutexName(path);
            
            try
            {
                mutex = new Mutex(true, mutexName, out var createdNew);
                
                if (!createdNew)
                {
                    // Another instance is running, notify it
                    logger.Information("Another instance is already running at this path");
                    NotifyExistingInstance(path);
                    return false;
                }

                // Start listening for notifications
                StartListening(path);
                return true;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to create mutex");
                return true; // Continue anyway
            }
        }

        private static string GetMutexName(string path)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
            var hashString = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            return $"BililiveRecorder_Desktop_{hashString}";
        }

        private static string GetPipeName(string path)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(path.ToLowerInvariant()));
            var hashString = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
            return $"BililiveRecorder_Desktop_Pipe_{hashString}";
        }

        private static void NotifyExistingInstance(string path)
        {
            try
            {
                var pipeName = GetPipeName(path);
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                client.Connect(1000);
                using var writer = new StreamWriter(client);
                writer.WriteLine("activate");
                writer.Flush();
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to notify existing instance");
            }
        }

        private static void StartListening(string path)
        {
            cts = new CancellationTokenSource();
            var pipeName = GetPipeName(path);
            
            listenerThread = new Thread(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(pipeName, PipeDirection.In);
                        server.WaitForConnection();
                        
                        using var reader = new StreamReader(server);
                        var message = reader.ReadLine();
                        
                        if (message == "activate")
                        {
                            NotificationReceived?.Invoke(null, EventArgs.Empty);
                        }
                    }
                    catch (Exception) when (cts.Token.IsCancellationRequested)
                    {
                        // Expected when cancelling
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.Warning(ex, "Error in single instance listener");
                    }
                }
            })
            {
                IsBackground = true,
                Name = "SingleInstanceListener"
            };
            listenerThread.Start();
        }

        public static void Cleanup()
        {
            try
            {
                cts?.Cancel();
                mutex?.ReleaseMutex();
                mutex?.Dispose();
                mutex = null;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Error during single instance cleanup");
            }
        }
    }
}
