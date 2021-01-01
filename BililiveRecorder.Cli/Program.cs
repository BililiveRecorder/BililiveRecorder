using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config.V1;
using BililiveRecorder.FlvProcessor;
using CommandLine;
using Newtonsoft.Json;
using NLog;

namespace BililiveRecorder.Cli
{
    internal class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static void Main(string[] _)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<FlvProcessorModule>();
            builder.RegisterModule<CoreModule>();
            builder.RegisterType<CommandConfigV1>().As<ConfigV1>().InstancePerMatchingLifetimeScope("recorder_root");
            var Container = builder.Build();
            var RootScope = Container.BeginLifetimeScope("recorder_root");

            var Recorder = RootScope.Resolve<IRecorder>();
            if (!Recorder.Initialize(Directory.GetCurrentDirectory()))
            {
                Console.WriteLine("Initialize Error");
                return;
            }

            Parser.Default
                .ParseArguments(() => (CommandConfigV1)Recorder.Config, Environment.GetCommandLineArgs())
                .WithParsed(Run);

            return;
            void Run(ConfigV1 option)
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
    }

    public partial class CommandConfigV1 : ConfigV1
    {
        [Option('i', "id", HelpText = "room id", Required = true)]
        public string _RoomList
        {
            set
            {
                var roomids = value.Split(',');
                this.RoomList.Clear();

                foreach (var roomid in roomids)
                {
                    var room = new RoomV1();
                    room.Roomid = int.Parse(roomid);
                    room.Enabled = false;
                    this.RoomList.Add(room);
                }
            }
        }

        [Option('o', "dir", Default = ".", HelpText = "Output directory", Required = false)]
        public new string WorkDirectory
        {
            get => base.WorkDirectory;
            set => base.WorkDirectory = value;
        }

        [Option("cookie", HelpText = "Provide custom cookies", Required = false)]
        public new string Cookie
        {
            get => base.Cookie;
            set => base.Cookie = value;
        }

        [Option("live_api_host", HelpText = "Use custom api host", Required = false)]
        public new string LiveApiHost
        {
            get => base.LiveApiHost;
            set => base.LiveApiHost = value;
        }

        [Option("record_filename_format", HelpText = "Recording name format", Required = false)]
        public new string RecordFilenameFormat
        {
            get => base.RecordFilenameFormat;
            set => base.RecordFilenameFormat = value;
        }
    }
}
