using System;
using System.Threading;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Scripting.Runtime;
using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using Serilog;

namespace BililiveRecorder.Core.Scripting
{
    public class UserScriptRunner
    {
        private const string RecorderEvents = "recorderEvents";
        private static readonly JsValue RecorderEventsString = RecorderEvents;
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
", new ParserOptions(@"internalSetup.js")).ParseScript();
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

            var parser = new JavaScriptParser(source!, new ParserOptions("userscript.js"));
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

        private static ILogger BuildLogger(ILogger logger)
        {
            var id = Interlocked.Increment(ref ExecutionId);
            return logger.ForContext<UserScriptRunner>().ForContext(nameof(ExecutionId), id);
        }

        private FunctionInstance? ExecuteScriptThenGetEventHandler(ILogger logger, string functionName)
        {
            var script = this.GetParsedScript();
            if (script is null)
                return null;

            var engine = this.CreateJintEngine(logger);
            engine.Execute(script);

            if (engine.Realm.GlobalObject.Get(RecorderEventsString) is not ObjectInstance events)
            {
                logger.Warning("[Script] recorderEvents 被修改为非 object");
                return null;
            }

            return events.Get(functionName) as FunctionInstance;
        }

        public void CallOnTest(ILogger logger, Action<string>? alert)
        {
            const string callbackName = "onTest";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null) return;

                _ = func.Engine.Call(func, new DelegateWrapper(func.Engine, alert ?? delegate { }));
            }
            catch (Exception ex)
            {
                log.Error(ex, $"执行脚本 {callbackName} 时发生错误");
                return;
            }
        }

        /// <summary>
        /// 过滤保存的弹幕
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="json">弹幕 JSON 文本</param>
        /// <returns>是否保存弹幕</returns>
        public bool CallOnDanmaku(ILogger logger, string json)
        {
            const string callbackName = "onDanmaku";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null) return true;

                var result = func.Engine.Call(func, json);

                return result.IsLooselyEqual(true);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"执行脚本 {callbackName} 时发生错误");
                return true;
            }
        }

        /// <summary>
        /// 获取直播流 URL
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="roomid">房间号</param>
        /// <returns>直播流 URL</returns>
        public string? CallOnFetchStreamUrl(ILogger logger, int roomid, int[] qnSetting)
        {
            const string callbackName = "onFetchStreamUrl";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null) return null;

                var input = new ObjectInstance(func.Engine);
                input.Set("roomid", roomid);
                input.Set("qn", JsValue.FromObject(func.Engine, qnSetting));

                var result = func.Engine.Call(func, input);

                switch (result)
                {
                    case JsString jsString:
                        return jsString.ToString();
                    case JsUndefined or JsNull:
                        return null;
                    default:
                        log.Warning($"{RecorderEvents}.{callbackName}() 返回了不支持的类型: {{ValueType}}", result.Type);
                        return null;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"执行脚本 {callbackName} 时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 在发送请求之前修改直播流 URL
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="originalUrl">原直播流地址</param>
        /// <returns>url: 新直播流地址<br/>ip: 可选的强制使用的 IP 地址</returns>
        public (string url, string? ip)? CallOnTransformStreamUrl(ILogger logger, string originalUrl)
        {
            const string callbackName = "onTransformStreamUrl";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null) return null;

                var result = func.Engine.Call(func, originalUrl);

                switch (result)
                {
                    case JsString jsString:
                        return (jsString.ToString(), null);
                    case ObjectInstance obj:
                        {
                            var url = obj.Get("url");

                            if (url is not JsString urlString)
                            {
                                log.Warning($"{RecorderEvents}.{callbackName}() 返回的 object 缺少 url 属性");
                                return null;
                            }

                            var ip = obj.Get("ip") as JsString;

                            return (urlString.ToString(), ip?.ToString());
                        }
                    case JsUndefined or JsNull:
                        return null;
                    default:
                        log.Warning($"{RecorderEvents}.{callbackName}() 返回了不支持的类型: {{ValueType}}", result.Type);
                        return null;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"执行脚本 {callbackName} 时发生错误");
                return null;
            }
        }
    }
}
