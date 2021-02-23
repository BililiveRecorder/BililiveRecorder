using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Parser;
using BililiveRecorder.Flv.Xml;
using Xunit;

namespace BililiveRecorder.Flv.UnitTests.Xml
{
    public class XmlTests
    {
        [Fact]
        public void Test1()
        {
            // TODO cleanup

            var source = new XmlFlvFile
            {
                Tags = new List<Tag>
                {
                    new Tag
                    {
                        Type = TagType.Script,
                        Size=4321,
                        ScriptData = new ScriptTagBody(new List<IScriptDataValue>
                        {
                            (ScriptDataString)"test1",
                            new ScriptDataObject
                            {
                                ["key1"] = (ScriptDataNumber)5,
                                ["key2"] = (ScriptDataString)"testTest"
                            }
                        })
                    },
                    new Tag
                    {
                        Type = TagType.Audio,
                        ScriptData = new ScriptTagBody(new List<IScriptDataValue>
                        {
                            (ScriptDataString)"test2",
                            new ScriptDataObject
                            {
                                ["key1"] = (ScriptDataNumber)5,
                                ["key2"] = (ScriptDataString)"testTest"
                            }
                        })
                    },
                    new Tag
                    {
                        Type = TagType.Audio,
                        Flag = TagFlag.Header,
                        BinaryData = new MemoryStream(new byte[]{0,1,2,3,4,5,6,7})
                    },
                    new Tag
                    {
                        Type = TagType.Video,
                        Flag = TagFlag.Header | TagFlag.Keyframe
                    },
                    new Tag
                    {
                        Type = TagType.Video,
                        Nalus = new List<H264Nalu>
                        {
                            new H264Nalu(0,156, H264NaluType.CodedSliceDataPartitionB),
                            new H264Nalu(198,13216, H264NaluType.Pps),
                            new H264Nalu(432154,432156, H264NaluType.FillerData),
                        }
                    }
                }
            };

            var serializer = new XmlSerializer(typeof(XmlFlvFile));

            var writer1 = new StringWriter();
            serializer.Serialize(writer1, source);
            var str1 = writer1.ToString();

            var obj1 = serializer.Deserialize(new StringReader(str1));

            var writer2 = new StringWriter();
            serializer.Serialize(writer2, obj1);
            var str2 = writer2.ToString();

            var obj2 = serializer.Deserialize(new StringReader(str1));

            var writer3 = new StringWriter();
            serializer.Serialize(writer3, obj2);
            var str3 = writer3.ToString();

            Assert.Equal(str1, str2);
            Assert.Equal(str2, str3);
        }

        [Fact(Skip = "Not ready")]
        public async Task Test2Async()
        {
            var PATH = @"";

            var reader = new FlvTagPipeReader(PipeReader.Create(File.OpenRead(PATH)), new TestRecyclableMemoryStreamProvider(), skipData: true, logger: null);

            var source = new XmlFlvFile
            {
                Tags = new List<Tag>()
            };

            while (true)
            {
                var tag = await reader.ReadTagAsync(default).ConfigureAwait(false);

                if (tag is null)
                    break;

                source.Tags.Add(tag);
            }

            var writer1 = new StringWriter();
            XmlFlvFile.Serializer.Serialize(writer1, source);
            var str1 = writer1.ToString();

        }
    }
}
