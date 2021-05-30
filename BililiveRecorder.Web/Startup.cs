using System.Diagnostics;
using BililiveRecorder.Core;
using BililiveRecorder.Web.Schemas;
using GraphQL;
using GraphQL.Server;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                .AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                .AddSingleton<RecorderSchema>()
                .AddSingleton(typeof(EnumerationGraphType<>))
                .AddSingleton<IDocumentExecuter, SubscriptionDocumentExecuter>()
                .AddGraphQL((options, provider) =>
                {
                    options.EnableMetrics = this.Environment.IsDevelopment() || Debugger.IsAttached;
                    var logger = provider.GetRequiredService<ILogger<Startup>>();
                    options.UnhandledExceptionDelegate = ctx => logger.LogWarning(ctx.OriginalException, "Unhandled GraphQL Exception");
                })
                //.AddSystemTextJson()
                .AddNewtonsoftJson()
                .AddWebSockets()
                .AddGraphTypes(typeof(RecorderSchema))
                ;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) => app
            .UseCors()
            .UseWebSockets()
            .UseGraphQLWebSockets<RecorderSchema>()
            .UseGraphQL<RecorderSchema>()
            .UseGraphQLPlayground()
            .UseGraphQLGraphiQL()
            .UseGraphQLAltair()
            .UseGraphQLVoyager()
            .Use(next => async context =>
            {
                if (context.Request.Path == "/")
                {
                    await context.Response.WriteAsync(@"<h1>to be done</h1><style>a{margin:5px}</style><a href=""/ui/playground"">Playground</a><a href=""/ui/graphiql"">GraphiQL</a><a href=""/ui/altair"">Altair</a><a href=""/ui/voyager"">Voyager</a>").ConfigureAwait(false);
                }
                else
                {
                    context.Response.Redirect("/");
                }
            })
            ;
    }
}
