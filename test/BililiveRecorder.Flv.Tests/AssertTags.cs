using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BililiveRecorder.Flv.Tests
{
    public static class AssertTags
    {
        public static void ShouldHaveLinearTimestamps(List<Tag> tags) =>
            Assert.True(tags.Any2((a, b) => (a.Timestamp <= b.Timestamp) && (b.Timestamp - a.Timestamp < 50)));

        public static void ShouldHaveFullHeaderTags(List<Tag> tags)
        {
            Assert.Equal(TagType.Script, tags[0].Type);
            Assert.Equal(0, tags[0].Timestamp);

            Assert.Equal(TagType.Video, tags[1].Type);
            Assert.Equal(0, tags[1].Timestamp);
            Assert.Equal(TagFlag.Header | TagFlag.Keyframe, tags[1].Flag);

            Assert.Equal(TagType.Audio, tags[2].Type);
            Assert.Equal(0, tags[2].Timestamp);
            Assert.Equal(TagFlag.Header, tags[2].Flag);

            Assert.Equal(TagType.Video, tags[3].Type);
            Assert.Equal(0, tags[3].Timestamp);
            Assert.Equal(TagFlag.Keyframe, tags[3].Flag);
        }

        public static void ShouldAlmostEqual(List<Tag> expectedTags, List<Tag> actualTags)
        {
            Assert.Equal(expectedTags.Count, actualTags.Count);

            for (var i = 0; i < expectedTags.Count; i++)
            {
                var expected = expectedTags[i];
                var actual = actualTags[i];

                Assert.NotSame(expected, actual);
                Assert.Equal(expected.Type, actual.Type);
                Assert.Equal(expected.Flag, actual.Flag);

                if (expected.IsScript())
                {
                    Assert.Equal(0, actual.Timestamp);
                }
                else if (expected.IsEnd())
                {
                    Assert.True(actual.IsEnd());
                }
                else if (expected.IsHeader())
                {
                    Assert.Equal(0, actual.Timestamp);

                    var expectedBinaryData = expected.BinaryDataForSerializationUseOnly;
                    if (!string.IsNullOrWhiteSpace(expectedBinaryData))
                    {
                        var actualBinaryData = actual.BinaryDataForSerializationUseOnly;
                        Assert.Equal(expectedBinaryData, actualBinaryData);
                    }
                }
                else
                {
                    Assert.Equal(expected.Timestamp, actual.Timestamp);
                    Assert.Equal(expected.Index, actual.Index);
                }
            }
        }

        public static void ShouldHaveSingleHeaderTagPerType(List<Tag> tags)
        {
            Assert.Single(tags.Where(x => x.Type == TagType.Script));
            Assert.Single(tags.Where(x => x.Type == TagType.Audio && x.Flag == TagFlag.Header));
            Assert.Single(tags.Where(x => x.Type == TagType.Video && x.Flag == (TagFlag.Header | TagFlag.Keyframe)));
        }
    }
}
