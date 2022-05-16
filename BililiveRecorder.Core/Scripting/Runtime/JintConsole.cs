using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Serilog;
using Serilog.Events;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintConsole : ObjectInstance
    {
        private readonly ILogger logger;

        private static readonly IReadOnlyList<string> templateMessageMap;
        private const int MaxTemplateSlotCount = 8;

        private readonly Dictionary<string, int> counters = new();
        private readonly Dictionary<string, Stopwatch> timers = new();

        static JintConsole()
        {
            var map = new List<string>();
            templateMessageMap = map;
            var b = new StringBuilder("[Script]");
            for (var i = 1; i <= MaxTemplateSlotCount; i++)
            {
                b.Append(" {Message");
                b.Append(i);
                b.Append("}");
                map.Add(b.ToString());
            }
        }

        public JintConsole(Engine engine, ILogger logger) : base(engine)
        {
            this.logger = logger?.ForContext<JintConsole>() ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override void Initialize()
        {
            Add("assert", this.Assert);
            Add("clear", this.Clear);
            Add("count", this.Count);
            Add("countReset", this.CountReset);
            Add("debug", this.BuildLogFunc(LogEventLevel.Debug));
            Add("error", this.BuildLogFunc(LogEventLevel.Error));
            Add("info", this.BuildLogFunc(LogEventLevel.Information));
            Add("log", this.BuildLogFunc(LogEventLevel.Information));
            Add("time", this.Time);
            Add("timeEnd", this.TimeEnd);
            Add("timeLog", this.TimeLog);
            Add("trace", this.BuildLogFunc(LogEventLevel.Information));
            Add("warn", this.BuildLogFunc(LogEventLevel.Warning));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Add(string name, Func<JsValue, JsValue[], JsValue> func)
            {
                this.FastAddProperty(name, new ClrFunctionInstance(this._engine, name, func), false, false, false);
            }
        }

        private string[] FormatToString(ReadOnlySpan<JsValue> values)
        {
            var result = new string[values.Length];
            var jsonSerializer = new Lazy<JsonSerializer>(() => new JsonSerializer(this._engine));

            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                var text = value switch
                {
                    JsString jsString => jsString.ToString(),
                    JsBoolean or JsNumber or JsBigInt or JsNull or JsUndefined => value.ToString(),
                    _ => jsonSerializer.Value.Serialize(values[i], Undefined, Undefined).ToString()
                };
                result[i] = text;
            }

            return result;
        }

        // TODO: Add call stack support
        // Workaround: use `new Error().stack` in js side
        // ref: https://github.com/sebastienros/jint/discussions/1115

        private Func<JsValue, JsValue[], JsValue> BuildLogFunc(LogEventLevel level)
        {
            return Log;
            JsValue Log(JsValue thisObject, JsValue[] arguments)
            {
                var messages = this.FormatToString(arguments);
                if (messages.Length > 0 && messages.Length <= MaxTemplateSlotCount)
                {
                    // Serilog quote "Catch a common pitfall when a single non-object array is cast to object[]"
                    // ref: https://github.com/serilog/serilog/blob/fabc2cbe637c9ddfa2d1ddc9f502df120f444acd/src/Serilog/Core/Logger.cs#L368
                    var values = messages.Cast<object>().ToArray();

                    // Note: this is the non-generic, `params object[]` version
                    // void Write(LogEventLevel level, string messageTemplate, params object[] propertyValues);
                    this.logger.Write(level: level, messageTemplate: templateMessageMap[messages.Length - 1], propertyValues: values);
                }
                else
                {
                    // void Write<T>(LogEventLevel level, string messageTemplate, T propertyValue);
                    this.logger.Write(level: level, messageTemplate: "[Script] {Messages}", propertyValue: messages);
                }
                return Undefined;
            }
        }

        private JsValue Assert(JsValue thisObject, JsValue[] arguments)
        {
            if (!arguments.At(0).IsLooselyEqual(true))
            {
                string[] messages;

                if (arguments.Length < 2)
                {
                    messages = Array.Empty<string>();
                }
                else
                {
                    messages = this.FormatToString(arguments.AsSpan(1));
                }

                this.logger.Error("[Script] Assertion failed: {Messages}", messages);
            }
            return Undefined;
        }

        private JsValue Clear(JsValue thisObject, JsValue[] arguments) => Undefined; // noop

        private JsValue Count(JsValue thisObject, JsValue[] arguments)
        {
            var name = arguments.Length > 0 ? arguments[0].ToString() : "default";

            if (this.counters.TryGetValue(name, out var count))
            {
                count++;
            }
            else
            {
                count = 1;
            }
            this.counters[name] = count;

            this.logger.Information("[Script] {CounterName}: {Count}", name, count);
            return Undefined;
        }

        private JsValue CountReset(JsValue thisObject, JsValue[] arguments)
        {
            var name = arguments.Length > 0 ? arguments[0].ToString() : "default";
            this.counters.Remove(name);
            this.logger.Information("[Script] {CounterName}: {Count}", name, 0);
            return Undefined;
        }

        private JsValue Time(JsValue thisObject, JsValue[] arguments)
        {
            var name = arguments.Length > 0 ? arguments[0].ToString() : "default";
            if (this.timers.ContainsKey(name))
            {
                this.logger.Warning("[Script] Timer {TimerName} already exists", name);
            }
            else
            {
                this.timers[name] = Stopwatch.StartNew();
            }
            return Undefined;
        }

        private JsValue TimeEnd(JsValue thisObject, JsValue[] arguments)
        {
            var name = arguments.Length > 0 ? arguments[0].ToString() : "default";
            if (this.timers.TryGetValue(name, out var timer))
            {
                timer.Stop();
                this.timers.Remove(name);
                this.logger.Information("[Script] {TimerName}: {ElapsedMilliseconds} ms", name, timer.ElapsedMilliseconds);
            }
            else
            {
                this.logger.Warning("[Script] Timer {TimerName} does not exist", name);
            }
            return Undefined;
        }

        private JsValue TimeLog(JsValue thisObject, JsValue[] arguments)
        {
            var name = arguments.Length > 0 ? arguments[0].ToString() : "default";
            if (this.timers.TryGetValue(name, out var timer))
            {
                this.logger.Information("[Script] {TimerName}: {ElapsedMilliseconds} ms", name, timer.ElapsedMilliseconds);
            }
            else
            {
                this.logger.Warning("[Script] Timer {TimerName} does not exist", name);
            }
            return Undefined;
        }
    }
}
