using System.Net.Http;
using System.Threading.Tasks;
using Jint;
using Jint.Native;
using Jint.Native.Function;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    public class JintFetchSync : FunctionInstance
    {
        private static readonly JsString functionName = new JsString("fetch");

        public JintFetchSync(Engine engine) : base(engine, engine.Realm, functionName)
        {
        }

        protected override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            var (promise, resolve, reject) = this._engine.RegisterPromise();

            var task = Task.Run(() =>
            {

            });
            var req = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            return promise;
        }
    }
}
