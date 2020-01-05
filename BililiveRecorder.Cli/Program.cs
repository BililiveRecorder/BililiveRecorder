using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using CommandLine;

namespace BililiveRecorder.Cli
{
    class Program
    {
        private static int roomid = 528819;
        private static IContainer Container { get; set; }
        private static ILifetimeScope RootScope { get; set; }
        private static IRecorder Recorder { get; set; }
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] _)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            Container = builder.Build();
            RootScope = Container.BeginLifetimeScope("recorder_root");
            Recorder = RootScope.Resolve<IRecorder>();
            if (!Recorder.Initialize(System.IO.Directory.GetCurrentDirectory()))
            {
                Console.WriteLine("Initialize Error");
                return;
            }
            Parser.Default
                .ParseArguments<CommandLineOption>(Environment.GetCommandLineArgs())
                .WithParsed(Run);
        }

        private static void Run(CommandLineOption option)
        {
            if (option.RoomID == 0) return;
            roomid = option.RoomID;
            Recorder.Config.AvoidTxy = true;

            if (Recorder.Where(r => r.RoomId == roomid).Count() == 0)
            {
                Recorder.AddRoom(roomid);
            }
            logger.Info("开始录播");
            Task.WhenAll(Recorder.Where(r => r.RoomId == roomid).Select(x => Task.Run(() => x.Start()))).Wait();
            Console.CancelKeyPress += (sender, e) =>
            {
                Task.WhenAll(Recorder.Where(r => r.RoomId == roomid).Select(x => Task.Run(() => x.StopRecord()))).Wait();
                logger.Info("停止录播");
            };
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
    }

    public class CommandLineOption
    {
        [Option('i', "id", Default = 0, HelpText = "room id", Required = false)]
        public int RoomID { get; set; }
    }
}