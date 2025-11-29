using System;
using System.IO;
using System.Text;
using Xunit;

namespace BililiveRecorder.Flv.Tests.FlvTests
{
    public class TagTests
    {
        [Fact]
        public void CloneTest()
        {
            var random = new Random();

            var source = new Tag()
            {
                Type = (TagType)random.Next(),
                Flag = (TagFlag)random.Next(),
                Index = random.Next(),
                Size = (uint)random.Next(),
                Timestamp = random.Next(),
                BinaryData = new MemoryStream(Encoding.UTF8.GetBytes("asdf" + random.Next())),
            };

            var result = source.Clone();

            Assert.NotSame(source, result);

            Assert.Equal(source.Type, result.Type);
            Assert.Equal(source.Flag, result.Flag);
            Assert.Equal(source.Index, result.Index);
            Assert.Equal(source.Size, result.Size);
            Assert.Equal(source.Timestamp, result.Timestamp);
            Assert.Equal(source.DataHash, result.DataHash);

            Assert.NotSame(source.BinaryData, result.BinaryData);
            Assert.Equal(source.BinaryDataForSerializationUseOnly, result.BinaryDataForSerializationUseOnly);
        }
    }
}
