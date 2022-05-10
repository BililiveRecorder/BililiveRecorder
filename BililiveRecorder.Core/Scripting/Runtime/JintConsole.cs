using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Serilog;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    public class JintConsole : ObjectInstance
    {
        private readonly ILogger logger;

        private readonly Dictionary<string, int> counters = new();
        private readonly Dictionary<string, Stopwatch> timers = new();

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
            Add("debug", this.Debug);
            Add("error", this.Error);
            Add("info", this.Info);
            Add("log", this.Log);
            Add("time", this.Time);
            Add("timeEnd", this.TimeEnd);
            Add("timeLog", this.TimeLog);
            Add("trace", this.Trace);
            Add("warn", this.Warn);

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

        private JsValue Assert(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.At(0).IsLooselyEqual(0))
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

        private JsValue Debug(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Debug("[Script] {Messages}", messages);
            return Undefined;
        }

        private JsValue Error(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Error("[Script] {Messages}", messages);
            return Undefined;
        }

        private JsValue Info(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Information("[Script] {Messages}", messages);
            return Undefined;
        }

        private JsValue Log(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Information("[Script] {Messages}", messages);
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

        private JsValue Trace(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Information("[Script] {Messages}", messages);
            return Undefined;
        }

        private JsValue Warn(JsValue thisObject, JsValue[] arguments)
        {
            var messages = this.FormatToString(arguments);
            this.logger.Warning("[Script] {Messages}", messages);
            return Undefined;
        }
    }
}
