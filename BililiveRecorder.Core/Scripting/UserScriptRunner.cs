using System;
using System.Threading;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Scripting.Runtime;
using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native.Function;
using Jint.Native.Object;
using Serilog;

namespace BililiveRecorder.Core.Scripting
{
    public class UserScriptRunner
    {
        private static int ExecutionId = 0;

        private readonly GlobalConfig config;
        private readonly Options jintOptions;

        private static readonly Script setupScript;

        private string? cachedScriptSource;
        private Script? cachedScript;

        static UserScriptRunner()
        {
            setupScript = new JavaScriptParser(@"
globalThis.recorderEvents = {};
").ParseScript();
        }

        public UserScriptRunner(GlobalConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            this.jintOptions = new Options()
                .CatchClrExceptions()
                .LimitRecursion(100)
                .RegexTimeoutInterval(TimeSpan.FromSeconds(2))
                .Configure(engine =>
                {
                    engine.Realm.GlobalObject.FastAddProperty("dns", new JintDns(engine), writable: false, enumerable: false, configurable: false);
                    engine.Realm.GlobalObject.FastAddProperty("dotnet", new JintDotnet(engine), writable: false, enumerable: false, configurable: false);
                    engine.Realm.GlobalObject.FastAddProperty("fetchSync", new JintFetchSync(engine), writable: false, enumerable: false, configurable: false);
                });
        }

        private Script? GetParsedScript()
        {
            var source = this.config.UserScript;

            if (this.cachedScript is not null)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    this.cachedScript = null;
                    this.cachedScriptSource = null;
                    return null;
                }
                else if (this.cachedScriptSource == source)
                {
                    return this.cachedScript;
                }
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var parser = new JavaScriptParser(source);
            var script = parser.ParseScript();

            this.cachedScript = script;
            this.cachedScriptSource = source;

            return script;
        }

        private Engine CreateJintEngine(ILogger logger)
        {
            var engine = new Engine(this.jintOptions);

            engine.Realm.GlobalObject.FastAddProperty("console", new JintConsole(engine, logger), writable: false, enumerable: false, configurable: false);

            engine.Execute(setupScript);

            return engine;
        }

        public string CallOnStreamUrl(ILogger logger, string originalUrl)
        {
            var log = BuildLogger(logger);

            var script = this.GetParsedScript();

            if (script is null)
            {
                return originalUrl;
            }

            var engine = this.CreateJintEngine(log);

            engine.Execute(script);

            if (engine.Realm.GlobalObject.Get("recorderEvents") is not ObjectInstance events)
            {
                log.Warning("脚本: recorderEvents 被修改为非 object");
                return originalUrl;
            }

            if (events.Get("onStreamUrl") is not FunctionInstance func)
            {
                return originalUrl;
            }

            var result = engine.Call(func, originalUrl);

            if (result.Type == Jint.Runtime.Types.String)
            {


            }
            else if (result.Type == Jint.Runtime.Types.Object)
            {

            }
            else
            {

            }

            throw new NotImplementedException();
        }

        private static ILogger BuildLogger(ILogger logger)
        {
            var id = Interlocked.Increment(ref ExecutionId);
            return logger.ForContext<UserScriptRunner>().ForContext(nameof(ExecutionId), id);
        }
    }
}
