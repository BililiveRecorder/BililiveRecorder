using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BililiveRecorder.Core.Api;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Scripting.Runtime;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
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
        private static readonly JintStorage sharedStorage = new();

        private string? cachedScriptSource;
        private Script? cachedScript;

        static UserScriptRunner()
        {
            setupScript = Engine.PrepareScript(@"
globalThis.recorderEvents = {};
", "internalSetup.js");
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
                    engine.Realm.GlobalObject.FastSetProperty("dns", new PropertyDescriptor(new JintDns(engine), writable: false, enumerable: false, configurable: false));
                    engine.Realm.GlobalObject.FastSetProperty("dotnet", new PropertyDescriptor(new JintDotnet(engine), writable: false, enumerable: false, configurable: false));
                    engine.Realm.GlobalObject.FastSetProperty("fetchSync", new PropertyDescriptor(new JintFetchSync(engine), writable: false, enumerable: false, configurable: false));
                    engine.Realm.GlobalObject.FastSetProperty("URL", new PropertyDescriptor(TypeReference.CreateTypeReference<JintURL>(engine), writable: false, enumerable: false, configurable: false));
                    engine.Realm.GlobalObject.FastSetProperty("URLSearchParams", new PropertyDescriptor(TypeReference.CreateTypeReference<JintURLSearchParams>(engine), writable: false, enumerable: false, configurable: false));
                    engine.Realm.GlobalObject.FastSetProperty("sharedStorage", new PropertyDescriptor(new ObjectWrapper(engine, sharedStorage), writable: false, enumerable: false, configurable: false));
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

            var script = Engine.PrepareScript(source!, "userscript.js");

            this.cachedScript = script;
            this.cachedScriptSource = source;

            return script;
        }

        private Engine CreateJintEngine(ILogger logger)
        {
            var engine = new Engine(this.jintOptions);

            engine.Realm.GlobalObject.FastSetProperty("console", new PropertyDescriptor(new JintConsole(engine, logger), writable: false, enumerable: false, configurable: false));

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
        public string? CallOnFetchStreamUrl(ILogger logger, int roomid, IReadOnlyList<StreamCodecQn> qnSetting)
        {
            const string callbackName = "onFetchStreamUrl";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null) return null;

                var input = new JsObject(func.Engine);
                input.Set("roomid", roomid);

                var qnList = new List<int>();
                var qnV2 = new JsArray(func.Engine, capacity: (uint)qnSetting.Count);
                foreach (var setting in qnSetting)
                {
                    if (!qnList.Contains(setting.Qn))
                    {
                        qnList.Add(setting.Qn);
                    }

                    var v2element = new JsObject(func.Engine);
                    v2element.Set("qn", setting.Qn);
                    v2element.Set("codec", setting.Codec switch
                    {
                        StreamCodec.AVC => "avc",
                        StreamCodec.HEVC => "hevc",
                        _ => "unknown"
                    });
                    qnV2.Push(v2element);
                }
                input.Set("qn", new JsArray(func.Engine, qnList.Select(x => new JsNumber(x)).ToArray()));
                input.Set("qn_v2", qnV2);

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

        /// <summary>
        /// 修改发给弹幕服务器的握手包 JSON
        /// </summary>
        /// <param name="logger">logger</param>
        /// <param name="room">对应的直播间</param>
        /// <param name="json">原握手包文本 JSON 数据</param>
        /// <returns>新的握手包JSON 或 null</returns>
        public string? CallOnDanmakuHandshake(ILogger logger, IRoom room, string json)
        {
            const string callbackName = "onDanmakuHandshake";
            var log = BuildLogger(logger);
            try
            {
                var func = this.ExecuteScriptThenGetEventHandler(log, callbackName);
                if (func is null)
                    return null;

                var roomInfo = new JintRoomInfo(func.Engine, room);

                var result = func.Engine.Call(func, roomInfo, json);

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
    }
}
