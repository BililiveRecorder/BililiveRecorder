using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;

namespace BililiveRecorder.Core.Templating
{
    public sealed class FileNameGenerator
    {
        private static readonly FluidParser parser;
        private static readonly IFluidTemplate defaultTemplate;

        private static readonly Random _globalRandom = new();
        [ThreadStatic] private static Random? _localRandom;
        private static Random Random
        {
            get
            {
                if (_localRandom == null)
                {
                    int seed;
                    lock (_globalRandom)
                    {
                        seed = _globalRandom.Next();
                    }
                    _localRandom = new Random(seed);
                }
                return _localRandom;
            }
        }

        private readonly IFileNameConfig config;
        private readonly ILogger logger;

        static FileNameGenerator()
        {
            parser = new FluidParser();
            parser.RegisterExpressionTag("random", async static (expression, writer, encoder, context) =>
            {
                var value = await expression.EvaluateAsync(context);
                var valueStr = value.ToStringValue();
                if (!int.TryParse(valueStr, out var count))
                    count = 3;

                var text = string.Empty;

                while (count > 0)
                {
                    var step = count > 9 ? 9 : count;
                    count -= step;
                    var num = Random.Next((int)Math.Pow(10, step));
                    text += num.ToString("D" + step);
                }

                await writer.WriteAsync(text);

                return Completion.Normal;
            });

            parser = parser.Compile();

            defaultTemplate = parser.Parse(DefaultConfig.Instance.FileNameRecordTemplate);
        }

        public FileNameGenerator(IFileNameConfig config, ILogger? logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.logger = logger?.ForContext<FileNameGenerator>() ?? Logger.None;
        }

        public FileNameTemplateOutput CreateFilePath(FileNameTemplateContext data)
        {
            var status = FileNameTemplateStatus.Success;
            string? errorMessage = null;
            string relativePath;
            string? fullPath;

            var workDirectory = this.config.WorkDirectory;
            var skipFullPath = workDirectory is null;

            var now = DateTimeOffset.Now;
            var templateOptions = new TemplateOptions
            {
                Now = () => now,
            };
            templateOptions.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            templateOptions.ValueConverters.Add(o => o is JContainer j ? new JContainerValue(j) : null);
            templateOptions.Filters.AddFilter("format_qn",
                static (FluidValue input, FilterArguments arguments, TemplateContext context)
                    => new StringValue(StreamQualityNumber.MapToString((int)input.ToNumberValue())));
            templateOptions.Filters.AddFilter("unescape",
                static (FluidValue input, FilterArguments arguments, TemplateContext context)
                    => new StringValue(System.Net.WebUtility.HtmlDecode(input.ToStringValue())));

            var context = new TemplateContext(data, templateOptions);

            if (!parser.TryParse(this.config.FileNameRecordTemplate, out var template, out var error))
            {
                this.logger.Warning("文件名模板格式不正确，请修改: {ParserError}", error);
                errorMessage = "文件名模板格式不正确，请修改: " + error;
                status = FileNameTemplateStatus.TemplateError;
                goto returnDefaultPath;
            }

            relativePath = template.Render(context);
            relativePath = RemoveInvalidFileName(relativePath);

            fullPath = workDirectory is null ? null : Path.GetFullPath(Path.Combine(workDirectory, relativePath));

            if (!skipFullPath && !CheckIsWithinPath(workDirectory!, fullPath!))
            {
                this.logger.Warning("录制文件位置超出允许范围，请检查设置。将写入到默认路径。");
                status = FileNameTemplateStatus.OutOfRange;
                errorMessage = "录制文件位置超出允许范围";
                goto returnDefaultPath;
            }

            var ext = Path.GetExtension(relativePath);
            if (!ext.Equals(".flv", StringComparison.OrdinalIgnoreCase))
            {
                this.logger.Warning("录播姬只支持 FLV 文件格式，将在录制文件后缀名 {ExtensionName} 后添加 {DotFlv}。", ext, ".flv");
                relativePath += ".flv";

                if (!skipFullPath)
                    fullPath += ".flv";
            }

            if (!skipFullPath && File.Exists(fullPath))
            {
                this.logger.Warning("录制文件名冲突，将写入到默认路径。");
                status = FileNameTemplateStatus.FileConflict;
                errorMessage = "录制文件名冲突";
                goto returnDefaultPath;
            }

            return new FileNameTemplateOutput(status, errorMessage, relativePath, fullPath);

returnDefaultPath:
            var defaultRelativePath = RemoveInvalidFileName(defaultTemplate.Render(context));
            var defaultFullPath = workDirectory is null ? null : Path.GetFullPath(Path.Combine(workDirectory, defaultRelativePath));

            return new FileNameTemplateOutput(status, errorMessage, defaultRelativePath, defaultFullPath);
        }

        private class JContainerValue : ObjectValueBase
        {
            public JContainerValue(JContainer value) : base(value)
            {
            }

            public override ValueTask<FluidValue> GetValueAsync(string name, TemplateContext context)
            {
                var j = (JContainer)this.Value;
                JToken? value;

                if (j is JObject jobject)
                {
                    value = jobject[name];
                }
                else
                {
                    return NilValue.Instance;
                }

                if (value is null)
                {
                    return NilValue.Instance;
                }
                else if (value is JContainer)
                {
                    return Create(value, context.Options);
                }
                else if (value is JValue jValue)
                {
                    return Create(jValue.Value, context.Options);
                }
                else
                {
                    // WHAT ARE YOU !?
                    return NilValue.Instance;
                }
            }

            public override ValueTask<FluidValue> GetIndexAsync(FluidValue index, TemplateContext context)
            {
                var j = (JContainer)this.Value;
                JToken? value;

                try
                {
                    if (index.Type == FluidValues.Number)
                    {
                        value = j[(int)index.ToNumberValue()];
                    }
                    else
                    {
                        value = j[index.ToStringValue()];
                    }
                }
                catch (Exception)
                {
                    return NilValue.Instance;
                }

                if (value is null)
                {
                    return NilValue.Instance;
                }
                else if (value is JContainer)
                {
                    return Create(value, context.Options);
                }
                else if (value is JValue jValue)
                {
                    return Create(jValue.Value, context.Options);
                }
                else
                {
                    // WHAT ARE YOU !?
                    return NilValue.Instance;
                }
            }
        }

        private static readonly Regex invalidDirectoryNameRegex = new Regex(@"([ .])([\\\/])", RegexOptions.Compiled);

        internal static string RemoveInvalidFileName(string input, bool ignore_slash = true)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                if (!ignore_slash || (c != '\\' && c != '/'))
                    input = input.Replace(c, '_');

            input = invalidDirectoryNameRegex.Replace(input, "$1_$2");

            return input;
        }

        private static readonly char[] separator = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        internal static bool CheckIsWithinPath(string parent, string child)
        {
            var fullParent = Path.GetFullPath(parent);
            var fullChild = Path.GetFullPath(child);

            var parentSegments = fullParent.Split(separator, StringSplitOptions.None).AsSpan();
            if (parentSegments[^1] == "")
            {
                parentSegments = parentSegments[..^1];
            }

            var childSegments = fullChild.Split(separator, StringSplitOptions.None).AsSpan();
            if (childSegments[^1] == "")
            {
                childSegments = childSegments[..^1];
            }

            if (parentSegments.Length >= childSegments.Length)
                return false;

            return childSegments[..parentSegments.Length].SequenceEqual(parentSegments);
        }
    }
}
