using System;
using System.Collections.Generic;
using BililiveRecorder.Core.Api.Http;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal static class JintWbi
    {
        public static ClrFunction Create(Engine engine, Wbi? wbi)
        {
            return new ClrFunction(engine, "wbiSign", (thisObj, arguments) => CallImpl(engine, wbi, arguments));
        }

        private static JsValue CallImpl(Engine engine, Wbi? wbi, JsValue[] arguments)
        {
            try
            {
                if (wbi is null)
                    throw new JavaScriptException(engine.Intrinsics.Error, "wbiSign is not available in this context.");

                if (arguments.Length == 0)
                    throw new JavaScriptException(engine.Intrinsics.Error, "1 argument required, but only 0 present.");

                var query = new List<KeyValuePair<string, string>>();
                foreach (var pair in ParseQuery(engine, arguments[0]))
                {
                    if (pair.Key == Wbi.WTS || pair.Key == Wbi.W_RID)
                        continue;

                    query.Add(pair);
                }

                long? ts = null;
                if (arguments.Length > 1)
                {
                    var number = TypeConverter.ToNumber(arguments[1]);
                    if (!double.IsNaN(number))
                        ts = (long)number;
                }

                var (tsStr, sign) = ts.HasValue ? wbi.Sign(query, ts.Value) : wbi.Sign(query);

                var result = new JsObject(engine);
                foreach (var pair in query)
                    result.Set(pair.Key, pair.Value);
                result.Set(Wbi.WTS, tsStr);
                result.Set(Wbi.W_RID, sign);

                return result;
            }
            catch (JavaScriptException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new JavaScriptException(engine.Intrinsics.Error, "wbiSign failed: " + ex.Message);
            }
        }

        private static List<KeyValuePair<string, string>> ParseQuery(Engine engine, JsValue value)
        {
            var result = new List<KeyValuePair<string, string>>();

            if (value is ObjectWrapper { Target: JintURLSearchParams searchParams })
            {
                AddEntries(result, searchParams);
                return result;
            }

            if (value.IsString() || value is JsObject)
            {
                AddEntries(result, new JintURLSearchParams(value));
                return result;
            }

            throw new JavaScriptException(engine.Intrinsics.Error, "Only a plain object, URLSearchParams or query string is supported as the 1st argument.");
        }

        private static void AddEntries(List<KeyValuePair<string, string>> list, JintURLSearchParams searchParams)
        {
            foreach (var entry in searchParams.Entries())
                list.Add(new KeyValuePair<string, string>(entry[0] ?? string.Empty, entry[1] ?? string.Empty));
        }
    }
}
