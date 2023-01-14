using System;
using System.Collections.Generic;
using BililiveRecorder.Flv.Amf;
using BililiveRecorder.Flv.Pipeline;
using BililiveRecorder.Flv.Pipeline.Actions;
using BililiveRecorder.Flv.Pipeline.Rules;
using Xunit;

namespace BililiveRecorder.Flv.Tests.RuleTests
{
    public class FfmpegDetectionRuleTests
    {
        [Theory]
        [MemberData(nameof(EndTagTestData))]
        public void ShouldDetectEndTag(bool expectEndTag, PipelineAction pipelineAction)
        {
            var rule = new FfmpegDetectionRule();
            var pipeline = new ProcessingPipelineBuilder().AddRule(rule).Build();

            var context = new FlvProcessingContext(pipelineAction, new Dictionary<object, object?>());

            pipeline(context);

            Assert.Equal(expectEndTag, rule.EndTagDetected);
        }

        public static IEnumerable<object[]> EndTagTestData()
        {
            yield return new object[] { true, new PipelineEndAction(new Tag()) };
            yield return new object[] { false, new PipelineScriptAction(new Tag()) };
            yield return new object[] { false, new PipelineHeaderAction(Array.Empty<Tag>()) };
            yield return new object[] { false, new PipelineDataAction(Array.Empty<Tag>()) };
            yield return new object[] { false, PipelineNewFileAction.Instance };
        }

        [Theory]
        [InlineData(true, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""Object"",""Value"":{""encoder"":{""Type"":""String"",""Value"":""Lavf56.40.101""}}}]")]
        [InlineData(true, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""EcmaArray"",""Value"":{""encoder"":{""Type"":""String"",""Value"":""Lavf56.40.101""}}}]")]
        [InlineData(true, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""Object"",""Value"":{""encoder"":{""Type"":""String"",""Value"":""Lavf56.40.101""}}},{""Type"":""Null""}]")]
        [InlineData(true, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""Object"",""Value"":{""encoder"":{""Type"":""String"",""Value"":""Lavf123""}}}]")]
        [InlineData(false, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""EcmaArray"",""Value"":{""encoder"":{""Type"":""String"",""Value"":""libobs xxxx""}}}]")]
        [InlineData(false, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""EcmaArray"",""Value"":{}}]")]
        [InlineData(false, @"[{""Type"":""String"",""Value"":""onMetaData""},{""Type"":""Object"",""Value"":{}}]")]
        [InlineData(false, @"[{""Type"":""String"",""Value"":""aaa""},{""Type"":""Object"",""Value"":{}}]")]
        public void ShouldDetectLvafEncoder(bool expectedValue, string metadataJson)
        {
            var rule = new FfmpegDetectionRule();
            var pipeline = new ProcessingPipelineBuilder().AddRule(rule).Build();

            var action = new PipelineScriptAction(new Tag
            {
                Type = TagType.Script,
                ScriptData = ScriptTagBody.Parse(metadataJson)
            });

            var context = new FlvProcessingContext(action, new Dictionary<object, object?>());

            pipeline(context);

            Assert.Equal(expectedValue, rule.LavfEncoderDetected);
        }
    }
}
