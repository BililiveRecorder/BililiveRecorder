using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AutoMapper;
using BililiveRecorder.Core;
using BililiveRecorder.Web.Graphql;
using BililiveRecorder.Web.Models.Rest;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Server;
using GraphQL.SystemReactive;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace BililiveRecorder.Web
{
    public class Startup
    {
        // TODO 减少引用的依赖数量

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            this.Configuration = configuration;
            this.Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // GraphQL API 使用 Newtonsoft.Json 会同步调用 System.IO.StreamReader.Peek
            // 来源是 GraphQL.Server.Transports.AspNetCore.NewtonsoftJson.GraphQLRequestDeserializer.DeserializeFromJsonBodyAsync
            // System.Text.Json 不使用构造函数传属性，所以无法创建 Optional<T> （需要加 attribute，回头改）
            // TODO 改 Optional<T>
            services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

            // 如果 IRecorder 没有被注册过才会添加，模拟调试用
            // 实际运行时在 BililiveRecorder.Web.Program 里会加上真的 IRecorder
            services.TryAddSingleton<IRecorder>(new FakeRecorderForWeb());

#if DEBUG
            // TODO 移动到一个单独的测试项目里
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<DataMappingProfile>());
            configuration.AssertConfigurationIsValid();
#endif

            services
                .AddAutoMapper(c => c.AddProfile<DataMappingProfile>())
                .AddCors(o => o.AddDefaultPolicy(policy =>
                {
                    policy
                    .SetIsOriginAllowed(origin => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("Content-Length")
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                    ;
                }));

            services
                .AddSingleton(new ManifestEmbeddedFileProvider(typeof(Startup).Assembly))
                .AddSingleton(sp => new CompositeFileProvider(sp.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider, sp.GetRequiredService<ManifestEmbeddedFileProvider>()));

            // Graphql API
            GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services)
                .AddServer(true)
                .AddWebSockets()
                .AddNewtonsoftJson()
                .AddSchema<RecorderSchema>()
                .AddSubscriptionDocumentExecuter()
                .AddDefaultEndpointSelectorPolicy()
                .AddGraphTypes(typeof(RecorderSchema).Assembly)
                .Configure<ErrorInfoProviderOptions>(opt => opt.ExposeExceptionStackTrace = this.Environment.IsDevelopment() || Debugger.IsAttached)
                .ConfigureExecution(options =>
                {
                    options.EnableMetrics = this.Environment.IsDevelopment() || Debugger.IsAttached;
                    var logger = options.RequestServices?.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = ctx => logger?.LogError(ctx.OriginalException, "Graphql Error");
                })
                ;

            // REST API
            services
                .AddSwaggerGen(c =>
                {
                    c.UseAllOfForInheritance();
                    c.UseOneOfForPolymorphism();

                    c.SwaggerDoc("brec", new OpenApiInfo
                    {
                        Title = "录播姬 REST API",
                        Description = "录播姬网站 [rec.danmuji.org](https://rec.danmuji.org/)  \n录播姬 GitHub [BililiveRecorder/BililiveRecorder](https://github.com/BililiveRecorder/BililiveRecorder)  \n\n" +
                        "除了 REST API 以外，录播姬还有 Graphql API 可以使用。\n\n" +
                        "API 中的 objectId 在重启后会重新生成，不保存到配置文件。",
                        Version = "v1"
                    });

                    var filePath = Path.Combine(AppContext.BaseDirectory, "BililiveRecorder.Web.xml");
                    c.IncludeXmlComments(filePath);
                })
                .AddRouting(c =>
                {
                    c.LowercaseUrls = true;
                    c.LowercaseQueryStrings = true;
                })
                .AddMvcCore(option =>
                {

                })
                .AddApiExplorer()
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            const string PAGE404 = "/404.html";

            if (app.ApplicationServices.GetService<DisableOpenAccessWarningConfig>() is null)
            {
                app.UseMiddleware<OpenAccessWarningMiddleware>();
            }

            app
                .UseCors()
                .UseMiddleware<BasicAuthMiddleware>()
                .UseWebSockets();

            app.Use(static next => async context =>
            {
                // 404 页面处理
                await next(context);

                if (!context.Response.HasStarted
                && !context.Response.ContentLength.HasValue
                && string.IsNullOrEmpty(context.Response.ContentType)
                && context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    var originalPath = context.Request.Path;
                    var originalQueryString = context.Request.QueryString;
                    try
                    {
                        if (originalPath.StartsWithSegments("/ui"))
                        {
                            context.Items["webui-spa-path"] = originalPath;
                            context.Request.Path = "/ui/";
                        }
                        else
                        {
                            context.Request.Path = PAGE404;
                        }

                        context.Request.QueryString = new QueryString();
                        await next(context);
                    }
                    finally
                    {
                        context.Request.Path = originalPath;
                        context.Request.QueryString = originalQueryString;
                    }
                }
            });

            app.UseRouting();

            static void OnPrepareResponse(StaticFileResponseContext context)
            {
                if (context.Context.Request.Path == PAGE404)
                {
                    context.Context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Context.Response.Headers.CacheControl = "no-cache";
                }
                else
                {
                    context.Context.Response.Headers.CacheControl = "max-age=86400, must-revalidate";
                }
            }

            var ctp = new FileExtensionContentTypeProvider();
            ctp.Mappings[".htm"] = "text/html; charset=utf-8";
            ctp.Mappings[".html"] = "text/html; charset=utf-8";
            ctp.Mappings[".css"] = "text/css; charset=utf-8";
            ctp.Mappings[".js"] = "text/javascript; charset=utf-8";
            ctp.Mappings[".mjs"] = "text/javascript; charset=utf-8";
            ctp.Mappings[".json"] = "application/json; charset=utf-8";

            ctp.Mappings[".mkv"] = "video/x-matroska";
            ctp.Mappings[".mk3d"] = "video/x-matroska";
            ctp.Mappings[".mka"] = "audio/x-matroska";
            ctp.Mappings[".mks"] = "video/x-matroska";
            ctp.Mappings[".srt"] = "text/plain";
            ctp.Mappings[".ass"] = "text/plain";
            ctp.Mappings[".ssa"] = "text/plain";

            var compositeFileProvider = app.ApplicationServices.GetRequiredService<CompositeFileProvider>();
            var sharedStaticFiles = new SharedOptions()
            {
                // 在运行的 exe 旁边新建一个 wwwroot 文件夹，会优先使用里面的内容，然后 fallback 到打包的资源文件
                FileProvider = compositeFileProvider,
                RedirectToAppendTrailingSlash = true,
            };

            app
                .UseDefaultFiles(new DefaultFilesOptions(sharedStaticFiles))
                .UseStaticFiles(new StaticFileOptions(sharedStaticFiles)
                {
                    ContentTypeProvider = ctp,
                    OnPrepareResponse = OnPrepareResponse
                })
                //.UseDirectoryBrowser(new DirectoryBrowserOptions(shared))
                ;

            try
            {
                if (app.ApplicationServices.GetService<BililiveRecorderFileExplorerSettings>()?.Enable ?? false)
                {
                    var sharedRecordingFiles = new SharedOptions
                    {
                        FileProvider = new PhysicalFileProvider(app.ApplicationServices.GetRequiredService<IRecorder>().Config.Global.WorkDirectory),
                        RequestPath = "/file",
                        RedirectToAppendTrailingSlash = true,
                    };
                    app
                        .UseStaticFiles(new StaticFileOptions(sharedRecordingFiles)
                        {
                            ContentTypeProvider = ctp,
                            ServeUnknownFileTypes = true,
                        })
                        .UseDirectoryBrowser(new DirectoryBrowserOptions(sharedRecordingFiles)
                        {
                            Formatter = new BililiveRecorderDirectoryFormatter(),
                        });
                }
            }
            catch (Exception) { }

            app
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();

                    endpoints.MapGraphQL<RecorderSchema>();
                    endpoints.MapGraphQLWebSockets<RecorderSchema>();

                    endpoints.MapSwagger();
                    endpoints.MapGraphQLPlayground("graphql/playground");
                    endpoints.MapGraphQLGraphiQL("graphql/graphiql");
                    endpoints.MapGraphQLAltair("graphql/altair");
                    endpoints.MapGraphQLVoyager("graphql/voyager");
                })
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("brec/swagger.json", "录播姬 REST API");
                })
                .Use(static next => static context =>
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                });
        }
    }
}
