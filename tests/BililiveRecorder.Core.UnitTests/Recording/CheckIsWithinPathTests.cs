using System.Collections.Generic;
using System.IO;
using BililiveRecorder.Core.Templating;
using Xunit;

namespace BililiveRecorder.Core.UnitTests.Recording
{
    public class CheckIsWithinPathTests
    {
        [Theory, MemberData(nameof(GetTestData))]
        public void RunTest(bool expectation, string parent, string child)
        {
            Assert.Equal(expectation, FileNameGenerator.CheckIsWithinPath(parent, child));
        }

        public static IEnumerable<object[]> GetTestData()
        {
            yield return new object[] { true, @"/path/a/", "/path/a/file.flv" };
            yield return new object[] { true, @"/path/a", "/path/a/file.flv" };

            yield return new object[] { true, @"/path/a/", "/path/a/b/file.flv" };
            yield return new object[] { true, @"/path/a", "/path/a/b/file.flv" };

            yield return new object[] { true, @"/path/a/", "/path/a/../a/file.flv" };
            yield return new object[] { true, @"/path/a", "/path/a/../a/file.flv" };

            yield return new object[] { false, @"/path/a/", "/path/a/../b/file.flv" };
            yield return new object[] { false, @"/path/a", "/path/a/../b/file.flv" };

            yield return new object[] { true, @"/", "/path/a/file.flv" };
            yield return new object[] { true, @"/", "/file.flv" };

            yield return new object[] { false, @"/path", "/path/a/../../../../file.flv" };
            yield return new object[] { false, @"/path", "/path../../../../file.flv" };

            yield return new object[] { true, @"/path/a/", "/path////a/file.flv" };
            yield return new object[] { true, @"/path/a", "/path////a/file.flv" };

            yield return new object[] { false, @"/path/", "/path/" };
            yield return new object[] { false, @"/path", "/path" };
            yield return new object[] { false, @"/path/", "/path" };
            yield return new object[] { false, @"/path", "/path/" };

            var isWindows = Path.DirectorySeparatorChar == '\\';

            yield return new object[] { isWindows && false, @"C:\", @"C:\" };
            yield return new object[] { isWindows && true, @"C:\", @"C:\foo" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\foo" };
            yield return new object[] { isWindows && false, @"C:\foo\", @"C:\foo" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\foo\" };
            yield return new object[] { isWindows && true, @"C:\foo\", @"C:\foo\bar\" };
            yield return new object[] { isWindows && true, @"C:\foo\", @"C:\foo\bar" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\FOO\bar" };
            yield return new object[] { isWindows && true, @"C:\foo", @"C:/foo/bar" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\foobar" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\foobar\baz" };
            yield return new object[] { isWindows && false, @"C:\foo\", @"C:\foobar\baz" };
            yield return new object[] { isWindows && false, @"C:\foobar", @"C:\foo\bar" };
            yield return new object[] { isWindows && false, @"C:\foobar\", @"C:\foo\bar" };
            yield return new object[] { isWindows && false, @"C:\foo", @"C:\foo\..\bar\baz" };
            yield return new object[] { isWindows && true, @"C:\bar", @"C:\foo\..\bar\baz" };
            yield return new object[] { isWindows && false, @"C:\barr", @"C:\foo\..\bar\baz" };
            yield return new object[] { isWindows && false, @"C:\foo\", @"D:\foo\bar" };
            yield return new object[] { isWindows && false, @"\\server1\vol1\foo", @"\\server1\vol1\foo" };
            yield return new object[] { isWindows && false, @"\\server1\vol1\foo", @"\\server1\vol1\bar" };
            yield return new object[] { isWindows && true, @"\\server1\vol1\foo", @"\\server1\vol1\foo\bar" };
            yield return new object[] { isWindows && false, @"\\server1\vol1\foo", @"\\server1\vol1\foo\..\bar" };
        }
    }
}
