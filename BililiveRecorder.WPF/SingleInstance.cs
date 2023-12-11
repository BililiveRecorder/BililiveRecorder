//using System;
//using System.Collections.Generic;
//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Ipc;
//using System.Runtime.Serialization.Formatters;
//using System.Text;
//using System.Threading;
//using System.Windows;
//using System.Windows.Threading;

#nullable enable
namespace BililiveRecorder.WPF
{
    // FIXME
    public static class SingleInstance
    {/*
        private static Mutex? singleInstanceMutex;
        private static IpcServerChannel? channel;

        public static event EventHandler? NotificationReceived;

        public static bool CheckMutex(string path)
        {
            const string RemoteServiceName = "SingleInstanceApplicationService";

            var b64path = Convert.ToBase64String(Encoding.UTF8.GetBytes(path)).Replace('+', '_').Replace('/', '-');
            var identifier = "BililiveRecorder:SingeInstance:" + b64path;

            singleInstanceMutex = new Mutex(true, identifier, out var createdNew);
            if (createdNew)
            {
                channel = new IpcServerChannel(new Dictionary<string, string>
                {
                    ["name"] = identifier,
                    ["portName"] = identifier,
                    ["exclusiveAddressUse"] = "false"
                }, new BinaryServerFormatterSinkProvider
                {
                    TypeFilterLevel = TypeFilterLevel.Full
                });

                ChannelServices.RegisterChannel(channel, true);
                RemotingServices.Marshal(new IPCRemoteService(), RemoteServiceName);
            }
            else
            {
                ChannelServices.RegisterChannel(new IpcClientChannel(), true);
                var remote = (IPCRemoteService)RemotingServices.Connect(typeof(IPCRemoteService), $"ipc://{identifier}/{RemoteServiceName}");
                remote?.Notify();
            }

            return createdNew;
        }

        public static void Cleanup()
        {
            if (channel != null)
            {
                ChannelServices.UnregisterChannel(channel);
                channel = null;
            }

            singleInstanceMutex?.Close();
            singleInstanceMutex = null;
        }

        private static void ActivateFirstInstanceCallback() => NotificationReceived?.Invoke(null, EventArgs.Empty);

        private class IPCRemoteService : MarshalByRefObject
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
            public void Notify() => Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)ActivateFirstInstanceCallback);
#pragma warning restore VSTHRD110 // Observe result of async calls
            public override object? InitializeLifetimeService() => null;
        }
        */
    }
}
