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
            return Undefined;
        }
    }
}
