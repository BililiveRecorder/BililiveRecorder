using System.Collections.Generic;
using BililiveRecorder.Core.Api.Http;
using BililiveRecorder.Core.Config.V3;
using BililiveRecorder.Core.Scripting;
using BililiveRecorder.Core.Scripting.Runtime;
using Jint;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Interop;
using Serilog;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Scripting
{
    public class JintWbiTests
    {
        private const string ImgKey = "7cd084941338484aae1ad9425b84077c";
        private const string SubKey = "4932caff0ff746eab6f01bf08b70ac45";
        private const string ExpectedSign = "38f74af0c2908b42d19c87710d71e0b2";

        private static Wbi CreateWbi()
        {
            var wbi = new Wbi();
            wbi.UpdateKey(ImgKey, SubKey);
            return wbi;
        }

        private static Engine CreateEngine(Wbi wbi)
        {
            var engine = new Engine();
            engine.SetValue("wbiSign", JintWbi.Create(engine, wbi));
            return engine;
        }

        [Fact]
        public void SignsPlainObject()
        {
            var engine = CreateEngine(CreateWbi());

            var result = (ObjectInstance)engine.Evaluate("wbiSign({ platform: 'web', web_location: '444.7' }, 1743671757)");

            Assert.Equal("1743671757", result.Get("wts").ToString());
            Assert.Equal(ExpectedSign, result.Get("w_rid").ToString());
            Assert.Equal("web", result.Get("platform").ToString());
            Assert.Equal("444.7", result.Get("web_location").ToString());
        }

        [Fact]
        public void SignsQueryString()
        {
            var engine = CreateEngine(CreateWbi());

            var result = (ObjectInstance)engine.Evaluate("wbiSign('platform=web&web_location=444.7', 1743671757)");

            Assert.Equal("1743671757", result.Get("wts").ToString());
            Assert.Equal(ExpectedSign, result.Get("w_rid").ToString());
        }

        [Fact]
        public void SignsURLSearchParams()
        {
            var engine = CreateEngine(CreateWbi());
            engine.SetValue("URLSearchParams", TypeReference.CreateTypeReference<JintURLSearchParams>(engine));

            var result = (ObjectInstance)engine.Evaluate("wbiSign(new URLSearchParams('platform=web&web_location=444.7'), 1743671757)");

            Assert.Equal("1743671757", result.Get("wts").ToString());
            Assert.Equal(ExpectedSign, result.Get("w_rid").ToString());
        }

        [Fact]
        public void FiltersUnsupportedCharactersForHashOnly()
        {
            var engine = CreateEngine(CreateWbi());

            var result = (ObjectInstance)engine.Evaluate("wbiSign({ platform: \"we!'()*b\", web_location: '444.7' }, 1743671757)");

            Assert.Equal(ExpectedSign, result.Get("w_rid").ToString());
            Assert.Equal("we!'()*b", result.Get("platform").ToString());
        }

        [Fact]
        public void IgnoresExistingSignatureFields()
        {
            var engine = CreateEngine(CreateWbi());

            var result = (ObjectInstance)engine.Evaluate("wbiSign({ platform: 'web', web_location: '444.7', wts: '1', w_rid: 'old' }, 1743671757)");

            Assert.Equal("1743671757", result.Get("wts").ToString());
            Assert.Equal(ExpectedSign, result.Get("w_rid").ToString());
        }

        [Fact]
        public void ThrowsWhenKeyIsNotSet()
        {
            var engine = CreateEngine(new Wbi());

            var ex = Assert.Throws<JavaScriptException>(() => engine.Evaluate("wbiSign({ platform: 'web' }, 1743671757)"));
            Assert.Contains("Key is not set", ex.Message);
        }

        [Fact]
        public void IsRegisteredOnUserScriptRunner()
        {
            var global = new GlobalConfig
            {
                UserScript = "recorderEvents.onTest = (alert) => { alert(JSON.stringify(wbiSign({ platform: 'web', web_location: '444.7' }, 1743671757))); };",
            };

            var runner = new UserScriptRunner(global, CreateWbi());
            string? captured = null;

            runner.CallOnTest(new LoggerConfiguration().CreateLogger(), message => captured = message);

            Assert.NotNull(captured);
            Assert.Contains($"\"w_rid\":\"{ExpectedSign}\"", captured);
            Assert.Contains("\"wts\":\"1743671757\"", captured);
        }
    }
}
