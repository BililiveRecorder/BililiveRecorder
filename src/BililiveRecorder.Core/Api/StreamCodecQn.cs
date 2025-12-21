namespace BililiveRecorder.Core.Api
{
    public readonly record struct StreamCodecQn
    {
        public StreamCodec Codec { get; init; }
        public int Qn { get; init; }

        public override string ToString() => $"{this.Codec}:{this.Qn}";
    }
}
