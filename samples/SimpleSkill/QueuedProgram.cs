using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Executors;
using Pantheon.Framework.Flow;
using Pantheon.Framework.FlowQueue;
using Pantheon.Framework.LlmClient;
using Pantheon.Framework.Storage;
using Pantheon.Framework.Template;

namespace SimpleSkill
{
    /// <summary>
    /// Sample program demonstrating the use of QueuedExecutor with InMemoryFlowQueue
    /// </summary>
    class QueuedProgram
    {
        static async Task MainAsync(string[] args)
        {
            // Set up logging
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = false;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<QueuedProgram>();
            logger.LogInformation("Starting Queued Executor Sample");

            // Create a mock LLM client
            var llmClient = new MockLlmClient(loggerFactory.CreateLogger<MockLlmClient>());
            llmClient.CompletionResponseFunc = request =>
            {
                // Simple mock response for the summarize template
                if (request.Prompt.Contains("summarize"))
                {
                    return "This is a summary of the provided text. It highlights the main points and presents them in a concise manner.";
                }
                return MockLlmClient.DefaultCompletionResponseFunc(request);
            };

            // Create the summarize template
            var templateContent = "Please summarize the following text:\n\n{{ Text }}\n\nProvide a concise summary:";
            var systemTemplateContent = "You are a helpful assistant that summarizes text.";

            var outputParser = new OutputParser<SummarizeResult>(response =>
                new SummarizeResult { Summary = response });

            var summarizeTemplate = new Template<SummarizeInput, SummarizeResult>(
                "summarize",
                templateContent,
                outputParser,
                systemTemplateContent);

            // Create a template runner
            var templateRunner = new TemplateRunner(
                llmClient,
                loggerFactory.CreateLogger<TemplateRunner>());

            // Create the summarize flow
            async IAsyncEnumerable<SummarizeElement> SummarizeFlowFunc(
                FlowRunContext<SummarizeResult> context,
                SummarizeInput input)
            {
                // Yield an element for the first step
                yield return new SummarizeElement
                {
                    Step = "Starting summarization process"
                };

                // Simulate longer processing time to demonstrate queue behavior
                await Task.Delay(2000);
                yield return new SummarizeElement
                {
                    Step = "Processing text..."
                };

                // Execute the template
                var result = await templateRunner.ExecuteAsync(
                    summarizeTemplate,
                    input,
                    100);

                // Set the result
                context.SetResult(result);

                // Yield an element for the completion step
                yield return new SummarizeElement
                {
                    Step = "Summarization complete"
                };
            }

            var summarizeFlow = new Flow<SummarizeInput, SummarizeElement, SummarizeResult>(
                "summarize",
                SummarizeFlowFunc,
                typeof(SummarizeInput),
                typeof(SummarizeElement),
                typeof(SummarizeResult));

            // Create the in-memory flow storage
            var flowStorage = new InMemoryFlowStorage();
            
            // Create the in-memory flow queue with a visibility timeout of 10 seconds
            var flowQueue = new InMemoryFlowQueue(visibilityTimeoutSeconds: 10);

            // Create a wrapper to adapt the specific flow to the generic interface
            IFlow<object, object, object> CreateGenericFlow(IFlow<SummarizeInput, SummarizeElement, SummarizeResult> specificFlow)
            {
                return new GenericFlowAdapter(specificFlow);
            }

            // Create the flows dictionary
            var flows = new Dictionary<string, IFlow<object, object, object>>
            {
                { "summarize", CreateGenericFlow(summarizeFlow) }
            };
            
            // Create the queued executor with a maximum of 2 concurrent flows
            var executor = new QueuedExecutor(
                flowStorage,
                flowQueue,
                flows,
                maxConcurrentFlows: 2,
                loggerFactory.CreateLogger<QueuedExecutor>());

            try
            {
                // Submit multiple flows to demonstrate queue behavior
                Console.WriteLine("Enter the number of flows to submit (1-5):");
                if (!int.TryParse(Console.ReadLine(), out int numFlows))
                {
                    numFlows = 3; // Default to 3 flows
                }
                numFlows = Math.Clamp(numFlows, 1, 5);

                Console.WriteLine($"Submitting {numFlows} flows...");
                
                var inputText = "The quick brown fox jumps over the lazy dog. " +
                               "This sentence is often used because it contains every letter in the English alphabet. " +
                               "It's a pangram that has been used for typing practice, font display, and other purposes " +
                               "where all letters of the alphabet need to be visible.";
                
                // Create a list to track the flow IDs
                var flowIds = new List<Guid>();
                
                // Submit the flows
                for (int i = 0; i < numFlows; i++)
                {
                    var input = new SummarizeInput { Text = $"Flow #{i+1}: {inputText}" };
                    var flowRunId = await executor.SubmitFlowAsync("summarize", input, "user123");
                    flowIds.Add(flowRunId);
                    Console.WriteLine($"Submitted flow #{i+1} with ID: {flowRunId}");
                }
                
                // Wait for all flows to complete
                bool allCompleted;
                do
                {
                    await Task.Delay(500);
                    allCompleted = true;
                    
                    foreach (var flowId in flowIds)
                    {
                        var status = await executor.GetFlowStatusAsync(flowId);
                        if (status != FlowRunStatus.Completed && status != FlowRunStatus.Failed && status != FlowRunStatus.Canceled)
                        {
                            allCompleted = false;
                            break;
                        }
                    }
                    
                    Console.Write(".");
                } while (!allCompleted);
                
                Console.WriteLine("\nAll flows completed!");
                
                // Display results for each flow
                foreach (var flowId in flowIds)
                {
                    Console.WriteLine($"\n--- Flow {flowId} ---");
                    
                    // Get the flow status
                    var status = await executor.GetFlowStatusAsync(flowId);
                    Console.WriteLine($"Status: {status}");
                    
                    // Get the elements
                    var elements = await executor.GetFlowElementsAsync(flowId);
                    Console.WriteLine("Elements:");
                    foreach (var element in elements)
                    {
                        if (element.Content is SummarizeElement summarizeElement)
                        {
                            Console.WriteLine($"  - {summarizeElement.Step}");
                        }
                    }
                    
                    // Get the result
                    var result = await executor.GetFlowResultAsync(flowId);
                    if (result is SummarizeResult summarizeResult)
                    {
                        Console.WriteLine("Summary:");
                        Console.WriteLine(summarizeResult.Summary);
                    }
                    else
                    {
                        Console.WriteLine("No result available");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing flows");
            }
            finally
            {
                // Properly stop the queue processor
                await ((QueuedExecutor)executor).StopAsync();
                logger.LogInformation("Queue processor stopped");
            }

            // Wait for user input before exiting
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
