using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Executors;
using Pantheon.Framework.Flow;
using Pantheon.Framework.FlowQueue;
using Pantheon.Framework.Storage;
using Pantheon.Framework.Api.Flows;

namespace Pantheon.Framework.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });

            // Register flow storage as a singleton
            services.AddSingleton<IFlowStorage, InMemoryFlowStorage>();
            
            // Register flow queue as a singleton
            services.AddSingleton<IFlowQueue>(provider => 
            {
                // Configure the visibility timeout (how long before a flow is considered stalled)
                int visibilityTimeoutSeconds = Configuration.GetValue<int>("FlowQueue:VisibilityTimeoutSeconds", 30);
                var logger = provider.GetRequiredService<ILogger<InMemoryFlowQueue>>();
                return new InMemoryFlowQueue(visibilityTimeoutSeconds, logger);
            });

            // Register executor as a singleton with an empty flow dictionary
            // In a real application, you would add flows to this dictionary
            services.AddSingleton<IExecutor>(provider =>
            {
                var flowStorage = provider.GetRequiredService<IFlowStorage>();
                var flowQueue = provider.GetRequiredService<IFlowQueue>();
                var loggerExecutor = provider.GetRequiredService<ILogger<QueuedExecutor>>();
                
                // Create sample flows for testing
                var echoFlow = new EchoFlow(provider.GetRequiredService<ILogger<EchoFlow>>());
                var simpleEchoFlow = new SimpleEchoFlow(provider.GetRequiredService<ILogger<SimpleEchoFlow>>());
                var debugFlow = new DebugFlow(provider.GetRequiredService<ILogger<DebugFlow>>());
                
                // Register the flows
                var flows = new Dictionary<string, IFlow<object, object, object>>
                {
                    { "echo", echoFlow },
                    { "simple-echo", simpleEchoFlow },
                    { "debug", debugFlow }
                };
                
                // Configure the maximum number of concurrent flows
                int maxConcurrentFlows = Configuration.GetValue<int>("FlowQueue:MaxConcurrentFlows", 5);
                
                return new QueuedExecutor(flowStorage, flowQueue, flows, maxConcurrentFlows, loggerExecutor);
            });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // Add Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Pantheon Framework API",
                    Version = "v1",
                    Description = "A framework for building AI skills with LLMs"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger - some versions might cause issues due to compatibility
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pantheon Framework API v1");
                // Use swagger as the route prefix
                c.RoutePrefix = "swagger";
            });

            app.UseRouting();
            app.UseCors();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
