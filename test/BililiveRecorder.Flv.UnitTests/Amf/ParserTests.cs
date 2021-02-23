using System.Collections.Generic;
using BililiveRecorder.Flv.Amf;
using FluentAssertions;
using Xunit;

namespace BililiveRecorder.Flv.UnitTests.Amf
{
    public class ParserTests
    {
        [Theory, MemberData(nameof(JsonData))]
        public void ParseJson(ScriptTagBody expectation, string input)
        {
            var result = ScriptTagBody.Parse(input);
            result.Should().BeEquivalentTo(expectation: expectation);
        }

        [Theory, MemberData(nameof(BinaryData))]
        public void ParseBinary(ScriptTagBody expectation, byte[] input)
        {
            var result = ScriptTagBody.Parse(input);
            result.Should().BeEquivalentTo(expectation: expectation);
        }

        public static IEnumerable<object[]> JsonData()
        {
            yield return new object[] {
                SerializationTests.CreateTestObject1(),
                "[{\"Type\":\"String\",\"Value\":\"test\"},{\"Type\":\"Object\",\"Value\":{\"bool_true\":{\"Type\":\"Boolean\",\"Value\":true},\"bool_false\":{\"Type\":\"Boolean\",\"Value\":false},\"date1\":{\"Type\":\"Date\",\"Value\":\"2021-02-08T14:43:58.257+00:00\"},\"date2\":{\"Type\":\"Date\",\"Value\":\"2345-03-14T07:08:09.012+04:00\"},\"ecmaarray\":{\"Type\":\"EcmaArray\",\"Value\":{\"element1\":{\"Type\":\"String\",\"Value\":\"element1\"},\"element2\":{\"Type\":\"String\",\"Value\":\"element2\"},\"element3\":{\"Type\":\"String\",\"Value\":\"element3\"}}},\"longstring1\":{\"Type\":\"LongString\",\"Value\":\"longstring1\"},\"longstring2\":{\"Type\":\"LongString\",\"Value\":\"longstring2\"},\"null\":{\"Type\":\"Null\"},\"number1\":{\"Type\":\"Number\",\"Value\":0.0},\"number2\":{\"Type\":\"Number\",\"Value\":197653.845},\"number3\":{\"Type\":\"Number\",\"Value\":-95.7},\"number4\":{\"Type\":\"Number\",\"Value\":5E-324},\"strictarray\":{\"Type\":\"StrictArray\",\"Value\":[{\"Type\":\"String\",\"Value\":\"element1\"},{\"Type\":\"String\",\"Value\":\"element2\"},{\"Type\":\"String\",\"Value\":\"element3\"}]},\"string1\":{\"Type\":\"String\",\"Value\":\"string1\"},\"string2\":{\"Type\":\"String\",\"Value\":\"string2\"},\"undefined\":{\"Type\":\"Undefined\"}}}]",
            };
            yield return new object[] {
                SerializationTests.CreateTestObject2(),
                "[{\"Type\":\"String\",\"Value\":\"test\"},{\"Type\":\"Boolean\",\"Value\":true},{\"Type\":\"Boolean\",\"Value\":false},{\"Type\":\"Number\",\"Value\":0.0},{\"Type\":\"Number\",\"Value\":-95.7}]",
            };
        }

        public static IEnumerable<object[]> BinaryData()
        {
            yield return new object[] {
                SerializationTests.CreateTestObject1(),
                new byte[]{2,0,4,116,101,115,116,3,0,9,98,111,111,108,95,116,114,117,101,1,1,0,10,98,111,111,108,95,102,97,108,115,101,1,0,0,5,100,97,116,101,49,11,66,119,120,33,150,75,16,0,0,0,0,5,100,97,116,101,50,11,66,165,137,121,64,147,104,0,0,240,0,9,101,99,109,97,97,114,114,97,121,8,0,0,0,3,0,8,101,108,101,109,101,110,116,49,2,0,8,101,108,101,109,101,110,116,49,0,8,101,108,101,109,101,110,116,50,2,0,8,101,108,101,109,101,110,116,50,0,8,101,108,101,109,101,110,116,51,2,0,8,101,108,101,109,101,110,116,51,0,0,9,0,11,108,111,110,103,115,116,114,105,110,103,49,12,0,0,0,11,108,111,110,103,115,116,114,105,110,103,49,0,11,108,111,110,103,115,116,114,105,110,103,50,12,0,0,0,11,108,111,110,103,115,116,114,105,110,103,50,0,4,110,117,108,108,5,0,7,110,117,109,98,101,114,49,0,0,0,0,0,0,0,0,0,0,7,110,117,109,98,101,114,50,0,65,8,32,174,194,143,92,41,0,7,110,117,109,98,101,114,51,0,192,87,236,204,204,204,204,205,0,7,110,117,109,98,101,114,52,0,0,0,0,0,0,0,0,1,0,11,115,116,114,105,99,116,97,114,114,97,121,10,0,0,0,3,2,0,8,101,108,101,109,101,110,116,49,2,0,8,101,108,101,109,101,110,116,50,2,0,8,101,108,101,109,101,110,116,51,0,7,115,116,114,105,110,103,49,2,0,7,115,116,114,105,110,103,49,0,7,115,116,114,105,110,103,50,2,0,7,115,116,114,105,110,103,50,0,9,117,110,100,101,102,105,110,101,100,6,0,0,9},
            };
            yield return new object[] {
                SerializationTests.CreateTestObject2(),
                new byte[]{ 2, 0, 4, 116, 101, 115, 116, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 192, 87, 236, 204, 204, 204, 204, 205 },
            };
        }
    }
}
