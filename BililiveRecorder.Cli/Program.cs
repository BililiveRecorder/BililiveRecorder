using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BililiveRecorder.Cli.Configure;
using BililiveRecorder.Core;
using BililiveRecorder.Core.Config;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.DependencyInjection;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.ToolBox;
using BililiveRecorder.Web;
using BililiveRecorder.Web.Models.Rest.Logs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Templates;

namespace BililiveRecorder.Cli
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BREC_SKIP_DISABLE_QUICK_EDIT")))
            {
                ConsoleModeHelper.SetQuickEditMode(false);
            }
            RootCommand root;

            using (var entrypointLogger = BuildLogger(LogEventLevel.Fatal, LogEventLevel.Verbose))
            {
                entrypointLogger.Information("Starting, {Version}, {CommandLine}", GitVersionInformation.InformationalVersion, args);
                try
                {
                    ServicePointManager.Expect100Continue = false;

                    var cmd_run = new Command("run", "Run BililiveRecorder in standard mode")
                    {
                        new Option<string?>(new []{ "--config-override" }, () => null, "Config path override"),
                        new Option<string?>(new []{ "--http-bind", "--bind", "-b" }, () => null, "Bind address for http service"),
                        new Option<string?>(new []{ "--http-basic-user" }, () => null, "Web interface username"),
                        new Option<string?>(new []{ "--http-basic-pass" }, () => null, "Web interface password"),
                        new Option<bool>(new []{ "--http-open-access" }, () => false, "Allow open access from the internet"),
                        new Option<bool>(new []{ "--enable-file-browser" }, () => true, "Enable file browser located at '/file'"),
                        new Option<LogEventLevel>(new []{ "--loglevel", "--log", "-l" }, () => LogEventLevel.Information, "Minimal log level output to console"),
                        new Option<LogEventLevel>(new []{ "--logfilelevel", "--flog" }, () => LogEventLevel.Debug, "Minimal log level output to file"),
                        new Option<string?>(new []{ "--cert-pem-path", "--pem" }, "Path of the certificate pem file"),
                        new Option<string?>(new []{ "--cert-key-path", "--key" }, "Path of the certificate key file"),
                        new Option<string?>(new []{ "--cert-pfx-path", "--pfx" }, "Path of the certificate pfx file"),
                        new Option<string?>(new []{ "--cert-password"}, "Password of the certificate"),

                        new Argument<string>("path"),
                    };
                    cmd_run.AddAlias("r");
                    cmd_run.Handler = CommandHandler.Create<RunModeArguments>(RunConfigModeAsync);

                    var cmd_portable = new Command("portable", "Run BililiveRecorder in config-less mode")
                    {
                        new Option<string?>(new []{ "--http-bind", "--bind", "-b" }, () => null, "Bind address for http service"),
                        new Option<string?>(new []{ "--http-basic-user" }, () => null, "Web interface username"),
                        new Option<string?>(new []{ "--http-basic-pass" }, () => null, "Web interface password"),
                        new Option<bool>(new []{ "--http-open-access" }, () => false, "Allow open access from the internet"),
                        new Option<bool>(new []{ "--enable-file-browser" }, () => true, "Enable file browser located at '/file'"),
                        new Option<LogEventLevel>(new []{ "--loglevel", "--log", "-l" }, () => LogEventLevel.Information, "Minimal log level output to console"),
                        new Option<LogEventLevel>(new []{ "--logfilelevel", "--flog" }, () => LogEventLevel.Debug, "Minimal log level output to file"),
                        new Option<string?>(new []{ "--cert-pem-path", "--pem" }, "Path of the certificate pem file"),
                        new Option<string?>(new []{ "--cert-key-path", "--key" }, "Path of the certificate key file"),
                        new Option<string?>(new []{ "--cert-pfx-path", "--pfx" }, "Path of the certificate pfx file"),
                        new Option<string?>(new []{ "--cert-password"}, "Password of the certificate"),

                        new Option<RecordMode>(new []{ "--record-mode", "--mode" }, () => RecordMode.Standard, "Recording mode"),
                        new Option<string>(new []{ "--cookie", "-c" }, "Cookie string for api requests"),
                        new Option<string>(new []{ "--filename", "-f" }, "File name format"),
                        new Option<PortableModeArguments.PortableDanmakuMode>(new []{ "--danmaku", "-d" }, "Flags for danmaku recording"),
                        new Option<string>("--webhook-url", "URL of webhoook"),
                        new Option<string>("--live-api-host"),
                        new Argument<string>("output-path"),
                        new Argument<int[]>("room-ids", () => Array.Empty<int>())
                    };
                    cmd_portable.AddAlias("p");
                    cmd_portable.Handler = CommandHandler.Create<PortableModeArguments>(RunPortableModeAsync);

                    root = new RootCommand("A Stream Recorder For Bilibili Live")
                    {
                        cmd_run,
                        cmd_portable,
                        new ConfigureCommand(),
                        new ToolCommand(),
                    };
                }
                catch (Exception ex)
                {
                    entrypointLogger.Fatal(ex, "Fatal error during startup");
                    return -1;
                }
            }

            return root.Invoke(args);
        }

        private static async Task<int> RunConfigModeAsync(RunModeArguments args)
        {
            var path = Path.GetFullPath(args.Path);

            using var logger = BuildLogger(args.LogLevel, args.LogFileLevel, enableWebLog: args.HttpBind is not null);
            Log.Logger = logger;

            path = Path.GetFullPath(path);

            ConfigV3? config;

            if (args.ConfigOverride is not null)
            {
                logger.Information("Using config from {ConfigOverride}", args.ConfigOverride);
                config = ConfigParser.LoadFromFile(args.ConfigOverride);
            }
            else
            {
                config = ConfigParser.LoadFromDirectory(path);
            }

            if (config is null)
            {
                logger.Error("Config Loading Failed");
                return -1;
            }

            config.Global.WorkDirectory = path;
            config.ConfigPathOverride = args.ConfigOverride;

            var serviceProvider = BuildServiceProvider(config, logger);

            return await RunRecorderAsync(serviceProvider, args);
        }

        private static async Task<int> RunPortableModeAsync(PortableModeArguments args)
        {
            using var logger = BuildLogger(args.LogLevel, args.LogFileLevel, enableWebLog: args.HttpBind is not null);
            Log.Logger = logger;

            var config = new ConfigV3()
            {
                DisableConfigSave = true,
            };

            {
                var global = config.Global;

                if (!string.IsNullOrWhiteSpace(args.Cookie))
                    global.Cookie = args.Cookie;

                if (!string.IsNullOrWhiteSpace(args.LiveApiHost))
                    global.LiveApiHost = args.LiveApiHost;

                if (!string.IsNullOrWhiteSpace(args.Filename))
                    global.FileNameRecordTemplate = args.Filename;

                if (!string.IsNullOrWhiteSpace(args.WebhookUrl))
                    global.WebHookUrlsV2 = args.WebhookUrl;

                global.RecordMode = args.RecordMode;

                var danmaku = args.Danmaku;
                global.RecordDanmaku = danmaku != PortableModeArguments.PortableDanmakuMode.None;
                global.RecordDanmakuSuperChat = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.SuperChat);
                global.RecordDanmakuGuard = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.Guard);
                global.RecordDanmakuGift = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.Gift);
                global.RecordDanmakuRaw = danmaku.HasFlag(PortableModeArguments.PortableDanmakuMode.RawData);

                global.WorkDirectory = Path.GetFullPath(args.OutputPath);
                config.Rooms = args.RoomIds.Select(x => new RoomConfig { RoomId = x, AutoRecord = true }).ToList();
            }

            var serviceProvider = BuildServiceProvider(config, logger);

            return await RunRecorderAsync(serviceProvider, args);
        }

        private static async Task<int> RunRecorderAsync(IServiceProvider serviceProvider, SharedArguments sharedArguments)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            IRecorder recorderAccessProxy(IServiceProvider x) => serviceProvider.GetRequiredService<IRecorder>();

            // recorder setup done
            // check if web service required
            IHost? host = null;
            if (sharedArguments.HttpBind is null)
            {
                logger.Information("Web API not enabled");
            }
            else
            {
#if DEBUG
                const LogEventLevel webLogEventLevel = LogEventLevel.Debug;
#else
                const LogEventLevel webLogEventLevel = LogEventLevel.Error;
#endif

                host = new HostBuilder()
                    .UseSerilog(logger: new LoggerConfiguration().MinimumLevel.Is(webLogEventLevel).WriteTo.Logger(logger).CreateLogger(), dispose: true)
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(recorderAccessProxy);

                        services.AddSingleton(new BililiveRecorderFileExplorerSettings(sharedArguments.EnableFileBrowser));

                        sharedArguments.HttpBasicUser ??= Environment.GetEnvironmentVariable("BREC_HTTP_BASIC_USER");
                        sharedArguments.HttpBasicPass ??= Environment.GetEnvironmentVariable("BREC_HTTP_BASIC_PASS");

                        if (sharedArguments.HttpBasicUser is not null || sharedArguments.HttpBasicPass is not null)
                        {
                            services.AddSingleton(new BasicAuthCredential(sharedArguments.HttpBasicUser ?? string.Empty, sharedArguments.HttpBasicPass ?? string.Empty));
                        }

                        if (sharedArguments.HttpOpenAccess || Environment.GetEnvironmentVariable("BREC_HTTP_OPEN_ACCESS") is not null){
                            services.AddSingleton(new DisableOpenAccessWarningConfig());
                        }
                    })
                    .ConfigureWebHost(webBuilder =>
                    {
                        webBuilder
                        .UseKestrel(option =>
                        {
                            (var scheme, var host, var port) = ParseBindArgument(sharedArguments.HttpBind, logger);

                            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                            {
                                option.ListenLocalhost(port, ListenConfigure);
                            }
                            else if (IPAddress.TryParse(host, out var ip))
                            {
                                option.Listen(ip, port, ListenConfigure);
                            }
                            else
                            {
                                option.ListenAnyIP(port, ListenConfigure);
                            }

                            void ListenConfigure(ListenOptions listenOptions)
                            {
                                if (scheme == "https")
                                {
                                    listenOptions.UseHttps(LoadCertificate(sharedArguments, logger) ?? GenerateSelfSignedCertificate(logger), https =>
                                    {
                                        https.SslProtocols = SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
                                    });
                                }
                            }
                        })
                        .UseStartup<Startup>();
                    })
                    .Build();
            }

            ConsoleCancelEventHandler p = null!;
            var cts = new CancellationTokenSource();
            p = (sender, e) =>
            {
                logger.Information("Ctrl+C pressed. Exiting");
                Console.CancelKeyPress -= p;
                e.Cancel = true;
                cts.Cancel();
            };
            Console.CancelKeyPress += p;

            IRecorder? recorder = null;

            try
            {
                var token = cts.Token;
                if (host is not null)
                {
                    try
                    {
                        await host.StartAsync(token);
                    }
                    catch (Exception ex)
                    {
                        logger.Fatal(ex, "Failed to start web server.");
                        return -1;
                    }
                    logger.Information("Web host started.");

                    recorder = serviceProvider.GetRequiredService<IRecorder>();

                    await Task.WhenAny(Task.Delay(-1, token), host.WaitForShutdownAsync()).ConfigureAwait(false);

                    logger.Information("Shutdown in progress.");

                    await host.StopAsync().ConfigureAwait(false);
                }
                else
                {
                    recorder = serviceProvider.GetRequiredService<IRecorder>();
                    await Task.Delay(-1, token).ConfigureAwait(false);
                }
            }
            finally
            {
                recorder?.Dispose();
                // TODO 修复这里 Dispose 之后不会停止房间继续初始化
            }
            await Task.Delay(1000 * 3).ConfigureAwait(false);
            return 0;
        }

        private static X509Certificate2? LoadCertificate(SharedArguments arguments, ILogger logger)
        {
            if (arguments.CertPfxPath is not null)
            {
                if (arguments.CertPemPath is not null || arguments.CertKeyPath is not null)
                {
                    logger.Warning("Both cert-pfx and cert-pem/cert-key are specified. Using cert-pfx.");
                }

                if (!File.Exists(arguments.CertPfxPath))
                {
                    logger.Error("Certificate file {Path} not found.", arguments.CertPfxPath);
                    return null;
                }

                return new X509Certificate2(arguments.CertPfxPath, arguments.CertPassword);
            }
            else if (arguments.CertPemPath is not null || arguments.CertKeyPath is not null)
            {
                if (arguments.CertPemPath is null)
                {
                    logger.Error("Certificate PEM file not specified.");
                    return null;
                }

                if (arguments.CertKeyPath is null)
                {
                    logger.Error("Certificate key file not specified.");
                    return null;
                }

                if (!File.Exists(arguments.CertPemPath))
                {
                    logger.Error("Certificate PEM file {Path} not found.", arguments.CertPemPath);
                    return null;
                }

                if (!File.Exists(arguments.CertKeyPath))
                {
                    logger.Error("Certificate key file {Path} not found.", arguments.CertKeyPath);
                    return null;
                }

                var cert = arguments.CertPassword is null
                    ? X509Certificate2.CreateFromPemFile(arguments.CertPemPath, arguments.CertKeyPath)
                    : X509Certificate2.CreateFromEncryptedPemFile(arguments.CertPemPath, arguments.CertPassword, arguments.CertKeyPath);

                return new X509Certificate2(cert.Export(X509ContentType.Pfx));
            }
            else
            {
                logger.Debug("No certificate specified.");
                return null;
            }
        }

        private static X509Certificate2 GenerateSelfSignedCertificate(ILogger logger)
        {
            logger.Warning("使用录播姬生成的自签名证书");

            var firstDayofCurrentYear = new DateTimeOffset(DateTime.Now.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
            X509Certificate2? CA = null;
            try
            {
                {
                    using var key = RSA.Create();
                    var req = new CertificateRequest("CN=自签名证书，每次启动都会重新生成", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, false));
                    CA = new X509Certificate2(req.CreateSelfSigned(firstDayofCurrentYear, firstDayofCurrentYear.AddYears(10)).Export(X509ContentType.Pfx));
                }

                {
                    using var key = RSA.Create();
                    var req = new CertificateRequest("CN=mikufans录播姬", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    var subjectAltName = new SubjectAlternativeNameBuilder();
                    subjectAltName.AddDnsName("BililiveRecorder");
                    subjectAltName.AddDnsName("localhost");
                    subjectAltName.AddIpAddress(IPAddress.Loopback);
                    subjectAltName.AddIpAddress(IPAddress.IPv6Loopback);
                    subjectAltName.AddDnsName("*.nip.io");
                    subjectAltName.AddDnsName("*.sslip.io");
                    req.CertificateExtensions.Add(subjectAltName.Build());

                    using var cert = req.Create(CA, firstDayofCurrentYear, firstDayofCurrentYear.AddYears(10), "BililiveRecorder".Select(x => (byte)x).ToArray());
                    using var withPrivateKey = cert.CopyWithPrivateKey(key);
                    return new X509Certificate2(withPrivateKey.Export(X509ContentType.Pfx));
                }
            }
            finally
            {
                CA?.Dispose();
            }
        }

        private static (string schema, string host, int port) ParseBindArgument(string bind, ILogger logger)
        {
            if (int.TryParse(bind, out var value))
            {
                // 只传入了一个端口号
                return ("http", "localhost", value);
            }

            var match = Regex.Match(bind, @"^(?<schema>https?):\/\/(?<host>[^\:\/\?\#]+)(?:\:(?<port>\d+))?(?:\/.*)?$", RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(5));
            if (match.Success)
            {
                var schema = match.Groups["schema"].Value.ToLower();
                var host = match.Groups["host"].Value;
                var port = match.Groups["port"].Success ? int.Parse(match.Groups["port"].Value) : 2356;
                return (schema, host, port);
            }
            else
            {
                logger.Warning("侦听参数解析失败，使用默认值 {DefaultBindLocation}", "http://localhost:2356");
                return ("http", "localhost", 2356);
            }
        }

        private static IServiceProvider BuildServiceProvider(ConfigV3 config, ILogger logger) => new ServiceCollection()
            .AddSingleton(logger)
            .AddFlv()
            .AddRecorderConfig(config)
            .AddRecorder()
            .BuildServiceProvider();

        private static Logger BuildLogger(LogEventLevel logLevel, LogEventLevel logFileLevel, bool enableWebLog = false)
        {
            var logFilePath = Environment.GetEnvironmentVariable("BILILIVERECORDER_LOG_FILE_PATH");
            if (string.IsNullOrWhiteSpace(logFilePath))
                logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "bilirec.txt");
            logFilePath = Path.GetFullPath(logFilePath);
            var logFilePathMicrosoft = Path.Combine(Path.GetDirectoryName(logFilePath)!, Path.GetFileNameWithoutExtension(logFilePath) + "-web" + Path.GetExtension(logFilePath));

            var matchMicrosoft = Matching.FromSource("Microsoft");

            var ansiColorSupport = !OperatingSystem.IsWindows() || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION"));

            var builder = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .Destructure.AsScalar<IPAddress>()
                .Destructure.AsScalar<ProcessingComment>()
                .Destructure.ByTransforming<Flv.Xml.XmlFlvFile.XmlFlvFileMeta>(x => new
                {
                    x.Version,
                    x.ExportTime,
                    x.FileSize,
                    x.FileCreationTime,
                    x.FileModificationTime,
                })
                .WriteTo.Logger(sl =>
                {
                    sl
                    .Filter.ByExcluding(matchMicrosoft)
                    .WriteTo.File(new CompactJsonFormatter(), logFilePath, restrictedToMinimumLevel: logFileLevel, shared: true, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                    ;
                })
                .WriteTo.Logger(sl =>
                {
                    sl
                    .Filter.ByIncludingOnly(matchMicrosoft)
                    .WriteTo.File(new CompactJsonFormatter(), logFilePathMicrosoft, restrictedToMinimumLevel: logFileLevel, shared: true, rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                    ;
                });

            if (enableWebLog)
            {
                var webSink = new WebApiLogEventSink(new CompactJsonFormatter());
                WebApiLogEventSink.Instance = webSink;
                builder.WriteTo.Logger(sl =>
                {
                    sl
                    .Filter.ByExcluding(matchMicrosoft)
                    .WriteTo.Async(l => l.Sink(webSink, restrictedToMinimumLevel: LogEventLevel.Debug));
                });
            }

            if (ansiColorSupport)
            {
                builder.WriteTo.Console(new ExpressionTemplate("[{@t:HH:mm:ss} {@l:u3}{#if SourceContext is not null} ({SourceContext}){#end}]{#if RoomId is not null} [{RoomId}]{#end} {@m}{#if ExceptionDetail is not null}\n    [{ExceptionDetail['Type']}]: {ExceptionDetail['Message']}{#end}\n", theme: Serilog.Templates.Themes.TemplateTheme.Code), logLevel);
            }
            else
            {
                builder.WriteTo.Console(restrictedToMinimumLevel: logLevel, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3} ({SourceContext})] [{RoomId}] {Message:lj}{NewLine}{Exception}");
            }

            return builder.CreateLogger();
        }

        public abstract class SharedArguments
        {
            public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

            public LogEventLevel LogFileLevel { get; set; } = LogEventLevel.Information;

            public string? HttpBind { get; set; } = null;

            public string? HttpBasicUser { get; set; } = null;

            public string? HttpBasicPass { get; set; } = null;

            public bool HttpOpenAccess { get; set; } = false;

            public bool EnableFileBrowser { get; set; }

            public string? CertPemPath { get; set; } = null;

            public string? CertKeyPath { get; set; } = null;

            public string? CertPfxPath { get; set; } = null;

            public string? CertPassword { get; set; } = null;
        }

        public sealed class RunModeArguments : SharedArguments
        {
            public string? ConfigOverride { get; set; } = null;

            public string Path { get; set; } = string.Empty;
        }

        public sealed class PortableModeArguments : SharedArguments
        {
            public RecordMode RecordMode { get; set; } = RecordMode.Standard;

            public string OutputPath { get; set; } = string.Empty;

            public string? Cookie { get; set; }

            public string? LiveApiHost { get; set; }

            public string? Filename { get; set; }

            public string? WebhookUrl { get; set; }

            public PortableDanmakuMode Danmaku { get; set; }

            public IEnumerable<int> RoomIds { get; set; } = Enumerable.Empty<int>();

            [Flags]
            public enum PortableDanmakuMode
            {
                None = 0,
                Danmaku = 1 << 0,
                SuperChat = 1 << 1,
                Guard = 1 << 2,
                Gift = 1 << 3,
                RawData = 1 << 4,
                All = Danmaku | SuperChat | Guard | Gift | RawData
            }
        }

        private static class ConsoleModeHelper
        {
            internal static bool SetQuickEditMode(bool enable = true)
            {
                return SetMode(ConsoleModes.ENABLE_QUICK_EDIT_MODE, enable);
            }

            private static bool SetMode(ConsoleModes mode, bool enable = true)
            {
                IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
                if (consoleHandle == INVALID_HANDLE_VALUE)
                {
                    return false;
                }

                uint consoleMode;
                if (!GetConsoleMode(consoleHandle, out consoleMode))
                {
                    return false;
                }

                if (enable)
                {
                    consoleMode |= (uint)mode;
                }
                else
                {
                    consoleMode &= ~(uint)mode;
                }

                return SetConsoleMode(consoleHandle, consoleMode);
            }

            const int STD_INPUT_HANDLE = -10;
            static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [Flags]
            private enum ConsoleModes : uint
            {
                ENABLE_PROCESSED_INPUT = 0x0001,
                ENABLE_LINE_INPUT = 0x0002,
                ENABLE_ECHO_INPUT = 0x0004,
                ENABLE_WINDOW_INPUT = 0x0008,
                ENABLE_MOUSE_INPUT = 0x0010,
                ENABLE_INSERT_MODE = 0x0020,
                ENABLE_QUICK_EDIT_MODE = 0x0040,
                ENABLE_EXTENDED_FLAGS = 0x0080,
                ENABLE_AUTO_POSITION = 0x0100,
                ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200,

                ENABLE_PROCESSED_OUTPUT = 0x0001,
                ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
                ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
                DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
                ENABLE_LVB_GRID_WORLDWIDE = 0x0010
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern IntPtr GetStdHandle(int nStdHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        }
    }
}
