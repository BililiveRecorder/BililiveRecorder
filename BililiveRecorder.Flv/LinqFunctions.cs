using BililiveRecorder.Flv.Pipeline.Actions;
using StructLinq;

namespace BililiveRecorder.Flv
{
    public static class LinqFunctions
    {
        public static TagIsHeaderStruct TagIsHeader;
        public readonly struct TagIsHeaderStruct : IFunction<Tag, bool>, IInFunction<Tag, bool>
        {
            public bool Eval(Tag element) => element.IsHeader();
            public bool Eval(in Tag element) => element.IsHeader();
        }

        public static TagIsDataStruct TagIsData;
        public readonly struct TagIsDataStruct : IFunction<Tag, bool>, IInFunction<Tag, bool>
        {
            public bool Eval(Tag element) => element.IsData();
            public bool Eval(in Tag element) => element.IsData();
        }

        public static TagIsVideoStruct TagIsVideo;
        public readonly struct TagIsVideoStruct : IFunction<Tag, bool>, IInFunction<Tag, bool>
        {
            public bool Eval(Tag element) => element.Type == TagType.Video;
            public bool Eval(in Tag element) => element.Type == TagType.Video;
        }

        public static TagIsAudioStruct TagIsAudio;
        public readonly struct TagIsAudioStruct : IFunction<Tag, bool>, IInFunction<Tag, bool>
        {
            public bool Eval(Tag element) => element.Type == TagType.Audio;
            public bool Eval(in Tag element) => element.Type == TagType.Audio;
        }

        public static SumSizeOfVideoDataStruct SumSizeOfVideoData;
        public readonly struct SumSizeOfVideoDataStruct : IFunction<PipelineDataAction, long>, IInFunction<PipelineDataAction, long>
        {
            public long Eval(PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Sum(ref SumSizeOfTagByProperty, x => x, x => x);
            public long Eval(in PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Sum(ref SumSizeOfTagByProperty, x => x, x => x);
        }

        public static SumSizeOfVideoDataByNaluStruct SumSizeOfVideoDataByNalu;
        public readonly struct SumSizeOfVideoDataByNaluStruct : IFunction<PipelineDataAction, long>, IInFunction<PipelineDataAction, long>
        {
            public long Eval(PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Sum(ref SumSizeOfTagByPropertyOrNalu, x => x, x => x);
            public long Eval(in PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Sum(ref SumSizeOfTagByPropertyOrNalu, x => x, x => x);
        }

        public static SumSizeOfAudioDataStruct SumSizeOfAudioData;
        public readonly struct SumSizeOfAudioDataStruct : IFunction<PipelineDataAction, long>, IInFunction<PipelineDataAction, long>
        {
            public long Eval(PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsAudio, x => x).Sum(ref SumSizeOfTagByProperty, x => x, x => x);
            public long Eval(in PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsAudio, x => x).Sum(ref SumSizeOfTagByProperty, x => x, x => x);
        }

        public static SumSizeOfTagByPropertyStruct SumSizeOfTagByProperty;
        public readonly struct SumSizeOfTagByPropertyStruct : IFunction<Tag, long>, IInFunction<Tag, long>
        {
            public long Eval(Tag element) => element.Size + (11L + 4L);
            public long Eval(in Tag element) => element.Size + (11L + 4L);
        }

        public static SumSizeOfTagByPropertyOrNaluStruct SumSizeOfTagByPropertyOrNalu;
        public readonly struct SumSizeOfTagByPropertyOrNaluStruct : IFunction<Tag, long>, IInFunction<Tag, long>
        {
            public long Eval(Tag element) => 11 + 4 + (element.Nalus == null ? element.Size : (5 + element.Nalus.ToStructEnumerable().Sum(ref SumSizeOfNalu, x => x, x => x)));
            public long Eval(in Tag element) => 11 + 4 + (element.Nalus == null ? element.Size : (5 + element.Nalus.ToStructEnumerable().Sum(ref SumSizeOfNalu, x => x, x => x)));
        }

        public static SumSizeOfNaluStruct SumSizeOfNalu;
        public readonly struct SumSizeOfNaluStruct : IFunction<H264Nalu, long>, IInFunction<H264Nalu, long>
        {
            public long Eval(H264Nalu element) => element.FullSize + 4;
            public long Eval(in H264Nalu element) => element.FullSize + 4;
        }

        public static CountVideoTagsStruct CountVideoTags;
        public readonly struct CountVideoTagsStruct : IFunction<PipelineDataAction, int>, IInFunction<PipelineDataAction, int>
        {
            public int Eval(PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Count();
            public int Eval(in PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsVideo, x => x).Count();
        }

        public static CountAudioTagsStruct CountAudioTags;
        public readonly struct CountAudioTagsStruct : IFunction<PipelineDataAction, int>, IInFunction<PipelineDataAction, int>
        {
            public int Eval(PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsAudio, x => x).Count();
            public int Eval(in PipelineDataAction element) => element.Tags.ToStructEnumerable().Where(ref TagIsAudio, x => x).Count();
        }
    }
}
