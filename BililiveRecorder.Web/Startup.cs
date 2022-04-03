using System;
using System.Diagnostics;
using System.IO;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.Configure<KestrelServerOptions>(o => o.AllowSynchronousIO = true);

            services.TryAddSingleton<IRecorder>(new FakeRecorderForWeb());

            services
                .AddAutoMapper(c =>
                {
                    c.AddProfile<DataMappingProfile>();
                })
                .AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                ;

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
                    options.UnhandledExceptionDelegate = ctx => logger?.LogError(ctx.OriginalException, "Graphql Error: {Error}");
                })
                ;

            // REST API
            services
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("brec", new OpenApiInfo
                    {
                        Title = "录播姬 REST API",
                        Description = "录播姬网站 [rec.danmuji.org](https://rec.danmuji.org/)  \n录播姬 GitHub [Bililive/BililiveRecorder](https://github.com/Bililive/BililiveRecorder)  \n\n" +
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) => app
            .UseCors()
            .UseWebSockets()
            .UseRouting()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapGraphQL<RecorderSchema>();
                endpoints.MapGraphQLWebSockets<RecorderSchema>();

                endpoints.MapSwagger();
                endpoints.MapGraphQLPlayground();
                endpoints.MapGraphQLGraphiQL();
                endpoints.MapGraphQLAltair();
                endpoints.MapGraphQLVoyager();

                endpoints.MapGet("/", async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(ConstStrings.HOME_PAGE_HTML, encoding: System.Text.Encoding.UTF8).ConfigureAwait(false);
                });

                endpoints.MapGet("favicon.ico", async context =>
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync(string.Empty);
                });
            })
            .UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("brec/swagger.json", "录播姬 REST API");
            })
            .Use(next => async context =>
            {
                context.Response.Redirect("/");
                await context.Response.WriteAsync(string.Empty);
            })
            ;
    }
}
