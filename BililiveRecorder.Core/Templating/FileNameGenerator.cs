using System;
using System.IO;
using System.Threading.Tasks;
using BililiveRecorder.Core.Config.V3;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Newtonsoft.Json.Linq;
using Serilog;

namespace BililiveRecorder.Core.Templating
{
    public class FileNameGenerator
    {
        private static readonly ILogger logger = Log.Logger.ForContext<FileNameGenerator>();

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

        private readonly GlobalConfig config;
        private IFluidTemplate? template;

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

        public FileNameGenerator(GlobalConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(config.FileNameRecordTemplate))
                {
                    this.UpdateTemplate();
                }
            };

            this.UpdateTemplate();
        }

        private void UpdateTemplate()
        {
            if (!parser.TryParse(this.config.FileNameRecordTemplate, out var template, out var error))
            {
                logger.Warning("文件名模板格式不正确，请修改: {ParserError}", error);
            }
            this.template = template;
        }

        public (string fullPath, string relativePath) CreateFilePath(FileNameContextData data)
        {
            var now = DateTimeOffset.Now;
            var templateOptions = new TemplateOptions
            {
                Now = () => now,
            };
            templateOptions.MemberAccessStrategy.MemberNameStrategy = MemberNameStrategies.CamelCase;
            templateOptions.ValueConverters.Add(o => o is JContainer j ? new JContainerValue(j) : null);
            templateOptions.Filters.AddFilter("format_qn", static (FluidValue input, FilterArguments arguments, TemplateContext context)
                => new StringValue(StreamQualityNumber.MapToString((int)input.ToNumberValue()))
                );

            var context = new TemplateContext(data, templateOptions);

            var workDirectory = this.config.WorkDirectory!;

            if (this.template is not { } t)
            {
                logger.ForContext(LoggingContext.RoomId, data.RoomId).Warning("文件名模板格式不正确，请检查设置。将写入到默认路径。");
                goto returnDefaultPath;
            }

            var relativePath = t.Render(context);
            relativePath = RemoveInvalidFileName(relativePath);
            var fullPath = Path.GetFullPath(Path.Combine(workDirectory, relativePath));

            if (!CheckIsWithinPath(workDirectory!, Path.GetDirectoryName(fullPath)))
            {
                logger.ForContext(LoggingContext.RoomId, data.RoomId).Warning("录制文件位置超出允许范围，请检查设置。将写入到默认路径。");
                goto returnDefaultPath;
            }

            var ext = Path.GetExtension(fullPath);
            if (!ext.Equals(".flv", StringComparison.OrdinalIgnoreCase))
            {
                logger.ForContext(LoggingContext.RoomId, data.RoomId).Warning("录播姬只支持 FLV 文件格式，将在录制文件后缀名 {ExtensionName} 后添加 .flv。", ext);
                relativePath += ".flv";
                fullPath += ".flv";
            }

            if (File.Exists(fullPath))
            {
                logger.ForContext(LoggingContext.RoomId, data.RoomId).Warning("录制文件名冲突，请检查设置。将写入到默认路径。");
                goto returnDefaultPath;
            }

            return (fullPath, relativePath);

        returnDefaultPath:
            var defaultRelativePath = RemoveInvalidFileName(defaultTemplate.Render(context));
            return (Path.GetFullPath(Path.Combine(this.config.WorkDirectory, defaultRelativePath)), defaultRelativePath);
        }

        public class FileNameContextData
        {
            public int RoomId { get; set; }

            public int ShortId { get; set; }

            public string Name { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public string AreaParent { get; set; } = string.Empty;

            public string AreaChild { get; set; } = string.Empty;

            public int Qn { get; set; }

            public JObject? Json { get; set; }
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

        internal static string RemoveInvalidFileName(string input, bool ignore_slash = true)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                if (!ignore_slash || c != '\\' && c != '/')
                    input = input.Replace(c, '_');
            return input;
        }

        internal static bool CheckIsWithinPath(string parent, string child)
        {
            if (parent is null || child is null)
                return false;

            parent = parent.Replace('/', '\\');
            if (!parent.EndsWith("\\"))
                parent += "\\";
            parent = Path.GetFullPath(parent);

            child = child.Replace('/', '\\');
            if (!child.EndsWith("\\"))
                child += "\\";
            child = Path.GetFullPath(child);

            return child.StartsWith(parent, StringComparison.Ordinal);
        }
    }
}
