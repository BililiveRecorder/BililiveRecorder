using System.Net;
using System.Net.Http;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintFetchSync : FunctionInstance
    {
        private static readonly JsString functionName = new JsString("fetchSync");

        public JintFetchSync(Engine engine) : base(engine, engine.Realm, functionName)
        {
        }

        protected override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            if (arguments.Length == 0)
                throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "1 argument required, but only 0 present.");

            if (arguments[0] is not JsString urlString)
                throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "Only url string is supported as the 1st argument.");

            ObjectInstance? initObject = null;
            if (arguments.Length > 1)
                initObject = arguments[1] is not ObjectInstance arg1
                    ? throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "The provided value is not of type 'RequestInit'.")
                    : arg1;

            var handler = new HttpClientHandler();
            var httpClient = new HttpClient(handler);
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, urlString.ToString());
            var throwOnRedirect = false;

            if (initObject is not null)
            {
                foreach (var kv in initObject.GetOwnProperties())
                {
                    var key = kv.Key;
                    var value = kv.Value;

                    if (!key.IsString())
                        continue;

                    switch (key.AsString())
                    {
                        case "body":
                            this.SetRequestBody(requestMessage, value.Value);
                            break;
                        case "headers":
                            this.SetRequestHeader(requestMessage, value.Value);
                            break;
                        case "method":
                            this.SetRequestMethod(requestMessage, value.Value);
                            break;
                        case "redirect":
                            {
                                var redirect = value.Value;
                                if (redirect is JsNull or JsUndefined)
                                    break;
                                switch (redirect.ToString())
                                {
                                    case "follow":
                                        handler.AllowAutoRedirect = true;
                                        break;
                                    case "manual":
                                        handler.AllowAutoRedirect = false;
                                        break;
                                    case "error":
                                        handler.AllowAutoRedirect = false;
                                        throwOnRedirect = true;
                                        break;
                                    default:
                                        throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, $"'{redirect}' is not a valid value for 'redirect'.");
                                }
                                break;
                            }
                        case "referrer":
                            {
                                var referrer = value.Value;
                                if (referrer is JsNull or JsUndefined)
                                    break;
                                requestMessage.Headers.Referrer = new System.Uri(referrer.ToString());
                                break;
                            }
                        case "cache":
                        case "credentials":
                        case "integrity":
                        case "keepalive":
                        case "mode":
                        case "referrerPolicy":
                        case "signal":
                        default:
                            break;
                    }
                }
            }

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var resp = httpClient.SendAsync(requestMessage).Result;

            if (throwOnRedirect && (resp.StatusCode is (HttpStatusCode)301 or (HttpStatusCode)302 or (HttpStatusCode)303 or (HttpStatusCode)307 or (HttpStatusCode)308))
            {
                throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, $"'Failed to fetch, Status code: {(int)resp.StatusCode}.");
            }

            var respString = resp.Content.ReadAsStringAsync().Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

            var respHeaders = new ObjectInstance(this._engine);
            foreach (var respHeader in resp.Headers)
                respHeaders.Set(respHeader.Key, string.Join(", ", respHeader.Value));

            var result = new ObjectInstance(this._engine);
            result.Set("body", respString);
            result.Set("headers", respHeaders);
            result.Set("ok", resp.IsSuccessStatusCode);
            result.Set("status", (int)resp.StatusCode);
            result.Set("statusText", resp.ReasonPhrase);
            return result;
        }

        private void SetRequestMethod(HttpRequestMessage requestMessage, JsValue value)
        {
            if (value is JsNull or JsUndefined)
                return;

            var method = value.ToString();
            requestMessage.Method = method.ToUpperInvariant() switch
            {
                "HEAD" => HttpMethod.Head,
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "PUT" => HttpMethod.Put,
                "DELETE" => HttpMethod.Delete,
                "OPTIONS" => HttpMethod.Options,
                "TRACE" => HttpMethod.Trace,
                _ => new HttpMethod(method),
            };
        }

        private void SetRequestHeader(HttpRequestMessage requestMessage, JsValue value)
        {
            if (value is JsNull or JsUndefined)
                return;

            if (value is ObjectInstance objectInstance)
            {
                foreach (var header in objectInstance.GetOwnProperties())
                {
                    var headerName = header.Key.ToString();
                    var headerValue = header.Value.Value.ToString();

                    requestMessage.Headers.Remove(headerName);
                    requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
            else if (value is ArrayInstance arrayInstance)
            {
                foreach (ArrayInstance header in arrayInstance)
                {
                    if (header.Length != 2)
                        throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "The header object must contain exactly two elements.");

                    var headerName = header[0].ToString();
                    var headerValue = header[1].ToString();

                    requestMessage.Headers.Remove(headerName);
                    requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
            else
            {
                throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "Only object or array is supported for 'header'.");
            }
        }

        private void SetRequestBody(HttpRequestMessage requestMessage, JsValue value)
        {
            if (value is JsNull or JsUndefined)
                return;

            if (value is not JsString jsString)
                throw new JavaScriptException(this._engine.Realm.Intrinsics.Error, "Only string is supported for 'body'.");

            requestMessage.Content = new StringContent(jsString.ToString());
        }
    }
}
