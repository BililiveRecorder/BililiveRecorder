using System.Collections.Generic;
using Xunit;

namespace BililiveRecorder.Core.UnitTests
{
    public class RoomIdFromUrlTests
    {
        [Theory]
        [MemberData(nameof(ShouldMatchData))]
        public void ShouldMatch(string id, string url)
        {
            var m = RoomIdFromUrl.Regex.Match(url);
            Assert.True(m.Success);
            Assert.Equal(id, m.Groups[1].Value);
        }

        public static TheoryData<string, string> ShouldMatchData()
        {
            var data = new TheoryData<string, string>();

            static IEnumerable<string> basePaths()
            {
                yield return "123";
                yield return "123?abc=def";
                yield return "123#anything";
                yield return "123#/";
                yield return "123?abc=def#anything";

                yield return "123/";
                yield return "123/?abc=def";
                yield return "123/#anything";
                yield return "123/#/";
                yield return "123/?abc=def#anything";
            }

            var prefix = new[]
            {
                "live.bilibili.com/",
                "http://live.bilibili.com/",
                "https://live.bilibili.com/",
                "live.bilibili.com/blanc/",
                "http://live.bilibili.com/blanc/",
                "https://live.bilibili.com/blanc/",
                "live.bilibili.com/h5/",
                "http://live.bilibili.com/h5/",
                "https://live.bilibili.com/h5/",
            };

            foreach (var p in prefix)
            {
                foreach (var b in basePaths())
                {
                    data.Add("123", p + b);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ShouldNotMatchData))]
        public void ShouldNotMatch(string url)
        {
            Assert.DoesNotMatch(RoomIdFromUrl.Regex, url);
        }

        public static TheoryData<string> ShouldNotMatchData()
        {
            var data = new TheoryData<string>
            {
                "https://live.bilibili.com/123/a",
                "https://live.bilibili.com/123a",
                "https://live.bilibili.com/123a/",
                "https://live.bilibili.com/notnumber",
                "live.bilibili.com/otherpage/123",
                "ftp://live.bilibili.com/123"
            };

            return data;
        }
    }
}
