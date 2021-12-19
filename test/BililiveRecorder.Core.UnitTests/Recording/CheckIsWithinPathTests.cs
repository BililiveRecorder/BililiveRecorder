using System.IO;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Recording
{
    public class CheckIsWithinPathTests
    {
        [Theory(Skip = "Path 差异")]
        [InlineData(@"C:\", @"C:\", false)]
        [InlineData(@"C:", @"C:\foo", true)]
        [InlineData(@"C:\", @"C:\foo", true)]
        [InlineData(@"C:\foo", @"C:\foo", false)]
        [InlineData(@"C:\foo\", @"C:\foo", false)]
        [InlineData(@"C:\foo", @"C:\foo\", true)]
        [InlineData(@"C:\foo\", @"C:\foo\bar\", true)]
        [InlineData(@"C:\foo\", @"C:\foo\bar", true)]
        [InlineData(@"C:\foo", @"C:\FOO\bar", false)]
        [InlineData(@"C:\foo", @"C:/foo/bar", true)]
        [InlineData(@"C:\foo", @"C:\foobar", false)]
        [InlineData(@"C:\foo", @"C:\foobar\baz", false)]
        [InlineData(@"C:\foo\", @"C:\foobar\baz", false)]
        [InlineData(@"C:\foobar", @"C:\foo\bar", false)]
        [InlineData(@"C:\foobar\", @"C:\foo\bar", false)]
        [InlineData(@"C:\foo", @"C:\foo\..\bar\baz", false)]
        [InlineData(@"C:\bar", @"C:\foo\..\bar\baz", true)]
        [InlineData(@"C:\barr", @"C:\foo\..\bar\baz", false)]
        [InlineData(@"C:\foo\", @"D:\foo\bar", false)]
        [InlineData(@"\\server1\vol1\foo", @"\\server1\vol1\foo", false)]
        [InlineData(@"\\server1\vol1\foo", @"\\server1\vol1\bar", false)]
        [InlineData(@"\\server1\vol1\foo", @"\\server1\vol1\foo\bar", true)]
        [InlineData(@"\\server1\vol1\foo", @"\\server1\vol1\foo\..\bar", false)]
        public void Test(string parent, string child, bool result)
        {
            // TODO fix path tests
            Assert.Equal(result, Core.Templating.FileNameGenerator.CheckIsWithinPath(parent, Path.GetDirectoryName(child)!));
        }
    }
}
