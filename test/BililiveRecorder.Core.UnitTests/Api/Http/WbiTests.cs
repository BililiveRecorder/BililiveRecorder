using System.Collections.Generic;
using BililiveRecorder.Core.Api.Http;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Api.Http
{
    public class WbiTests
    {
        private readonly Wbi wbi;

        public WbiTests()
        {
            this.wbi = new Wbi();
        }

        [Fact]
        public void TestSign()
        {
            this.wbi.UpdateKey("7cd084941338484aae1ad9425b84077c", "4932caff0ff746eab6f01bf08b70ac45");

            var query = new Dictionary<string, string>
            {
                ["platform"] = "web",
                ["web_location"] = "444.7",
            };

            const long ts = 1743671757;
            var result = this.wbi.Sign(query, ts);
            Assert.Equal("38f74af0c2908b42d19c87710d71e0b2", result.sign);
            Assert.Equal("1743671757", result.ts);
        }

        [Fact]
        public void TestSignFiltered()
        {
            this.wbi.UpdateKey("7cd084941338484aae1ad9425b84077c", "4932caff0ff746eab6f01bf08b70ac45");

            var query = new Dictionary<string, string>
            {
                ["platform"] = "we!'()*b",
                ["web_location"] = "444.7",
            };

            const long ts = 1743671757;
            var result = this.wbi.Sign(query, ts);
            Assert.Equal("38f74af0c2908b42d19c87710d71e0b2", result.sign);
            Assert.Equal("1743671757", result.ts);
        }
    }
}
