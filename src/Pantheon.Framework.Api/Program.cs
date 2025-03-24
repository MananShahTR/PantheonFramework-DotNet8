using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Executors;

namespace Pantheon.Framework.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            // Register application lifetime handlers
            host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(() =>
            {
                try
                {
                    // Get the executor instance
                    var executor = host.Services.GetService<Pantheon.Framework.Core.Interfaces.IExecutor>();
                    
                    // If it's a QueuedExecutor, we need to shut it down properly
                    if (executor is QueuedExecutor queuedExecutor)
                    {
                        // Stop the queue processor
                        queuedExecutor.StopAsync().GetAwaiter().GetResult();
                        Console.WriteLine("Queue processor stopped successfully");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping queue processor: {ex.Message}");
                }
            });
            
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                });
    }
}
