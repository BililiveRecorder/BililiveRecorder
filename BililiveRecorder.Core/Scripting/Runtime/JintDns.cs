using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;

namespace BililiveRecorder.Core.Scripting.Runtime
{
    internal class JintDns : ObjectInstance
    {
        public JintDns(Engine engine) : base(engine)
        {
        }

        protected override void Initialize()
        {
            Add("lookup", this.Lookup);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Add(string name, Func<JsValue, JsValue[], JsValue> func)
            {
                this.FastAddProperty(name, new ClrFunctionInstance(this._engine, name, func), false, false, false);
            }
        }

        private JsValue Lookup(JsValue thisObject, JsValue[] arguments)
        {
            string[] result;
            try
            {
                result = Dns.GetHostAddresses(arguments.At(0).AsString()).Select(x => x.ToString()).ToArray();
            }
            catch (Exception)
            {
                result = Array.Empty<string>();
            }

            return FromObject(this._engine, result);
        }
    }
}
