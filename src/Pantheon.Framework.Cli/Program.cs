using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Executors;
using Pantheon.Framework.Storage;

namespace Pantheon.Framework.Cli
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Set up the root command
            var rootCommand = new RootCommand("Pantheon CLI tool for managing LLM flows")
            {
                new Command("run", "Run a flow")
                {
                    new Argument<string>("flowName", "Name of the flow to run"),
                    new Option<string>(new[] { "--input", "-i" }, "Input file (JSON)") { IsRequired = true },
                    new Option<string>(new[] { "--user", "-u" }, () => "anonymous", "User ID")
                },
                new Command("status", "Get the status of a flow run")
                {
                    new Argument<Guid>("flowRunId", "ID of the flow run")
                },
                new Command("elements", "Get the elements produced by a flow run")
                {
                    new Argument<Guid>("flowRunId", "ID of the flow run")
                },
                new Command("result", "Get the result of a flow run")
                {
                    new Argument<Guid>("flowRunId", "ID of the flow run")
                },
                new Command("cancel", "Cancel a flow run")
                {
                    new Argument<Guid>("flowRunId", "ID of the flow run")
                }
            };

            // Set up the service provider
            var serviceProvider = ConfigureServices();

            // Set up the run command handler
            rootCommand.GetCommand("run").Handler = CommandHandler.Create<string, string, string>(async (flowName, input, user) =>
            {
                var executor = serviceProvider.GetRequiredService<IExecutor>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Read the input file
                    var inputData = JsonDocument.Parse(File.ReadAllText(input)).RootElement;
                    
                    // Submit the flow
                    logger.LogInformation("Submitting flow {FlowName}", flowName);
                    var flowRunId = await executor.SubmitFlowAsync(flowName, inputData, user);
                    
                    // Wait for the flow to complete
                    FlowRunStatus status;
                    do
                    {
                        await Task.Delay(100);
                        status = await executor.GetFlowStatusAsync(flowRunId);
                        Console.Write(".");
                    } while (status != FlowRunStatus.Completed && status != FlowRunStatus.Failed && status != FlowRunStatus.Canceled);
                    
                    Console.WriteLine();
                    
                    // Print the flow status
                    logger.LogInformation("Flow {FlowRunId} {Status}", flowRunId, status);
                    
                    if (status == FlowRunStatus.Completed)
                    {
                        // Get the result
                        var result = await executor.GetFlowResultAsync(flowRunId);
                        Console.WriteLine("Result:");
                        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        Console.WriteLine("Flow did not complete successfully");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running flow {FlowName}", flowName);
                    return 1;
                }
                
                return 0;
            });

            // Set up the status command handler
            rootCommand.GetCommand("status").Handler = CommandHandler.Create<Guid>(async (flowRunId) =>
            {
                var executor = serviceProvider.GetRequiredService<IExecutor>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Get the flow status
                    var status = await executor.GetFlowStatusAsync(flowRunId);
                    logger.LogInformation("Flow {FlowRunId} {Status}", flowRunId, status);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting flow status {FlowRunId}", flowRunId);
                    return 1;
                }
                
                return 0;
            });

            // Set up the elements command handler
            rootCommand.GetCommand("elements").Handler = CommandHandler.Create<Guid>(async (flowRunId) =>
            {
                var executor = serviceProvider.GetRequiredService<IExecutor>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Get the flow elements
                    var elements = await executor.GetFlowElementsAsync(flowRunId);
                    logger.LogInformation("Flow {FlowRunId} has {ElementCount} elements", flowRunId, elements.Count);
                    
                    // Print the elements
                    Console.WriteLine("Elements:");
                    foreach (var element in elements)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(element.Content, new JsonSerializerOptions { WriteIndented = true }));
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting flow elements {FlowRunId}", flowRunId);
                    return 1;
                }
                
                return 0;
            });

            // Set up the result command handler
            rootCommand.GetCommand("result").Handler = CommandHandler.Create<Guid>(async (flowRunId) =>
            {
                var executor = serviceProvider.GetRequiredService<IExecutor>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Get the flow result
                    var result = await executor.GetFlowResultAsync(flowRunId);
                    
                    if (result != null)
                    {
                        logger.LogInformation("Flow {FlowRunId} has a result", flowRunId);
                        
                        // Print the result
                        Console.WriteLine("Result:");
                        Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    else
                    {
                        logger.LogInformation("Flow {FlowRunId} has no result", flowRunId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting flow result {FlowRunId}", flowRunId);
                    return 1;
                }
                
                return 0;
            });

            // Set up the cancel command handler
            rootCommand.GetCommand("cancel").Handler = CommandHandler.Create<Guid>(async (flowRunId) =>
            {
                var executor = serviceProvider.GetRequiredService<IExecutor>();
                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    // Cancel the flow
                    bool canceled = await executor.CancelFlowAsync(flowRunId);
                    
                    if (canceled)
                    {
                        logger.LogInformation("Flow {FlowRunId} was canceled", flowRunId);
                    }
                    else
                    {
                        logger.LogWarning("Flow {FlowRunId} could not be canceled", flowRunId);
                        return 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error canceling flow {FlowRunId}", flowRunId);
                    return 1;
                }
                
                return 0;
            });

            // Parse the command line arguments
            return await rootCommand.InvokeAsync(args);
        }

        private static ServiceProvider ConfigureServices()
        {
            // Configure the service collection
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            // Add flow storage
            services.AddSingleton<IFlowStorage, InMemoryFlowStorage>();

            // Add executor with an empty flow dictionary
            // In a real application, you would register flows here
            services.AddSingleton<IExecutor>(provider =>
            {
                var flowStorage = provider.GetRequiredService<IFlowStorage>();
                var logger = provider.GetRequiredService<ILogger<InMemoryExecutor>>();
                var flows = new Dictionary<string, IFlow<object, object, object>>();
                
                return new InMemoryExecutor(flowStorage, flows, logger);
            });

            // Build the service provider
            return services.BuildServiceProvider();
        }
    }
}
