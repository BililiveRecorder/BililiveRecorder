using System;
using System.Collections.Generic;
using Xunit;

namespace BililiveRecorder.Flv.Tests.FlvTests
{
    public class Int24Tests
    {
        private static IEnumerable<object[]> TestData()
        {
            yield return new object[] { 0, new byte[] { 0, 0, 0 } };
            yield return new object[] { 1, new byte[] { 0, 0, 1 } };
            yield return new object[] { -1, new byte[] { 0xFF, 0xFF, 0xFF } };
            yield return new object[] { -8388608, new byte[] { 0x80, 0, 0 } };
            yield return new object[] { 8388607, new byte[] { 0x7F, 0xFF, 0xFF } };
            yield return new object[] { -5517841, new byte[] { 0xAB, 0xCD, 0xEF } };
        }

        [Theory, MemberData(nameof(TestData))]
        public void Int24SerializeCorrectly(int number, byte[] bytes)
        {
            var result = new byte[3];
            Int24.WriteInt24(result, number);
            Assert.Equal(bytes, result);
        }

        [Theory, MemberData(nameof(TestData))]
        public void Int24DeserializeCorrectly(int number, byte[] bytes)
        {
            var result = Int24.ReadInt24(bytes);
            Assert.Equal(number, result);
        }

        [Theory]
        [InlineData(8388608)]
        [InlineData(-8388609)]
        public void Int24ThrowOnOutOfRange(int number)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var result = new byte[3];
                Int24.WriteInt24(result, number);
            });
        }
    }
}
