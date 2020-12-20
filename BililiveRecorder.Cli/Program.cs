using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.FlvProcessor;
using CommandLine;
using Newtonsoft.Json;
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
            builder.RegisterType<CommandConfigV1>().As<ConfigV1>().InstancePerMatchingLifetimeScope("recorder_root");
            Container = builder.Build();

            RootScope = Container.BeginLifetimeScope("recorder_root");
            Recorder = RootScope.Resolve<IRecorder>();
            if (!Recorder.Initialize(System.IO.Directory.GetCurrentDirectory()))
            {
                Console.WriteLine("Initialize Error");
                return;
            }

            Parser.Default
                .ParseArguments<CommandConfigV1>(() => (CommandConfigV1)Recorder.Config, Environment.GetCommandLineArgs())
                .WithParsed(Run);
        }

        private static void Run(ConfigV1 option)
        {
            option.EnabledFeature = EnabledFeature.RecordOnly;
            foreach (var room in option.RoomList)
            {
                if (Recorder.Where(r => r.RoomId == room.Roomid).Count() == 0)
                {
                    Recorder.AddRoom(room.Roomid);
                }
            }
            
            logger.Info("Using workDir: " + option.WorkDirectory + "\n\tconfig: " + JsonConvert.SerializeObject(option, Formatting.Indented));

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

    public partial class CommandConfigV1 : ConfigV1
    {
        [Option('i', "id", HelpText = "room id", Required = true)]
        public string _RoomList
        {
            set
            {
                var roomids = value.Split(',');
                RoomList.Clear();

                foreach (var roomid in roomids)
                {
                    var room = new RoomV1();
                    room.Roomid = Int32.Parse(roomid);
                    room.Enabled = false;
                    RoomList.Add(room);
                }
            }

        }

        [Option('o', "dir", Default = ".", HelpText = "Output directory", Required = false)]
        public new string WorkDirectory
        {
            get { return base.WorkDirectory; }
            set { base.WorkDirectory = value; }
        }

        [Option("cookie", HelpText = "Provide custom cookies", Required = false)]
        public new string Cookie
        {
            get { return base.Cookie; }
            set { base.Cookie = value; }
        }

        [Option("avoidtxy", HelpText = "Avoid Tencent Cloud server", Required = false)]
        public new bool AvoidTxy
        {
            get { return base.AvoidTxy; }
            set { base.AvoidTxy = value; }
        }

        [Option("live_api_host", HelpText = "Use custom api host", Required = false)]
        public new string LiveApiHost
        {
            get { return base.LiveApiHost; }
            set { base.LiveApiHost = value; }
        }

        [Option("record_filename_format", HelpText = "Recording name format", Required = false)]
        public new string RecordFilenameFormat
        {
            get { return base.RecordFilenameFormat; }
            set { base.RecordFilenameFormat = value; }
        }
    }
}