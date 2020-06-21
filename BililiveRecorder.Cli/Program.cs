using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.FlvProcessor;
using CommandLine;
using NLog;

namespace BililiveRecorder.Cli
{
    class Program
    {
        private static IContainer Container { get; set; }
        private static ILifetimeScope RootScope { get; set; }
        private static IRecorder Recorder { get; set; }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
            Recorder.Config.AvoidTxy = true;

            if (option.RoomID != null)
            {
                int roomid = (int)option.RoomID;
                if (Recorder.Where(r => r.RoomId == roomid).Count() == 0)
                {
                    Recorder.AddRoom(roomid);
                }
            }
            
            
            logger.Info("开始录播");
            Task.WhenAll(Recorder.Select(x => Task.Run(() => x.Start()))).Wait();
            Console.CancelKeyPress += (sender, e) =>
            {
                Task.WhenAll(Recorder.Select(x => Task.Run(() => x.StopRecord()))).Wait();
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
        [Option('i', "id", HelpText = "room id", Required = false)]
        public int? RoomID { get; set; }
    }
}