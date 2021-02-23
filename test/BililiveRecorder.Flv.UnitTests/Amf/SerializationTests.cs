using System;
using System.Collections.Generic;
using BililiveRecorder.Flv.Amf;
using FluentAssertions;
using Xunit;

namespace BililiveRecorder.Flv.UnitTests.Amf
{
    public class SerializationTests
    {
        [Theory, MemberData(nameof(GetTestData))]
        public void EqualAfterJsonSerialization(ScriptTagBody input)
        {
            var json = input.ToJson();
            var body2 = ScriptTagBody.Parse(json);
            var json2 = body2.ToJson();

            body2.Should().BeEquivalentTo(input, options => options.RespectingRuntimeTypes());
            json2.Should().Be(json);
        }

        [Theory, MemberData(nameof(GetTestData))]
        public void EqualAfterBinarySerialization(ScriptTagBody input)
        {
            var bytes = input.ToBytes();
            var body2 = ScriptTagBody.Parse(bytes);
            var bytes2 = body2.ToBytes();

            body2.Should().BeEquivalentTo(input, options => options.RespectingRuntimeTypes());
            bytes2.Should().BeEquivalentTo(bytes2, options => options.RespectingRuntimeTypes());
        }

        [Theory, MemberData(nameof(GetTestData))]
        public void EqualAfterMixedSerialization(ScriptTagBody input)
        {
            var a_json = input.ToJson();
            var a_body = ScriptTagBody.Parse(a_json);
            var a_byte = a_body.ToBytes();

            var b_byte = input.ToBytes();
            var b_body = ScriptTagBody.Parse(b_byte);
            var b_json = b_body.ToJson();

            b_json.Should().Be(a_json);
            a_byte.Should().BeEquivalentTo(b_byte);

            a_body.Should().BeEquivalentTo(input, options => options.RespectingRuntimeTypes());
            b_body.Should().BeEquivalentTo(input, options => options.RespectingRuntimeTypes());
            a_body.Should().BeEquivalentTo(b_body, options => options.RespectingRuntimeTypes());
        }

        public static IEnumerable<object[]> GetTestData()
        {
            yield return new object[] { CreateTestObject1() };
            yield return new object[] { CreateTestObject2() };
        }

        public static ScriptTagBody CreateTestObject1() => new ScriptTagBody(new List<IScriptDataValue> {
            (ScriptDataString) "test",
            new ScriptDataObject
            {
                ["bool_true"] = (ScriptDataBoolean)true,
                ["bool_false"] = (ScriptDataBoolean)false,
                ["date1"] = (ScriptDataDate)DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), // remove extra precision
                ["date2"] = (ScriptDataDate)new DateTimeOffset(2345, 3, 14, 7, 8, 9, 12, TimeSpan.FromHours(4)),
                ["ecmaarray"] = new ScriptDataEcmaArray
                {
                    ["element1"] = (ScriptDataString)"element1",
                    ["element2"] = (ScriptDataString)"element2",
                    ["element3"] = (ScriptDataString)"element3",
                },
                ["longstring1"] = (ScriptDataLongString)"longstring1",
                ["longstring2"] = (ScriptDataLongString)"longstring2",
                ["null"] = new ScriptDataNull(),
                ["number1"] = (ScriptDataNumber)0,
                ["number2"] = (ScriptDataNumber)197653.845,
                ["number3"] = (ScriptDataNumber)(-95.7),
                ["number4"] = (ScriptDataNumber)double.Epsilon,
                ["strictarray"] = new ScriptDataStrictArray
                    {
                        (ScriptDataString)"element1",
                        (ScriptDataString)"element2",
                        (ScriptDataString)"element3",
                    },
                ["string1"] = (ScriptDataString)"string1",
                ["string2"] = (ScriptDataString)"string2",
                ["undefined"] = new ScriptDataUndefined(),
            }
        });

        public static ScriptTagBody CreateTestObject2() => new ScriptTagBody(new List<IScriptDataValue>
        {
            (ScriptDataString) "test",
            (ScriptDataBoolean)true,
            (ScriptDataBoolean)false,
            (ScriptDataNumber)0,
            (ScriptDataNumber)(-95.7),
        });
    }
}
