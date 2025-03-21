﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Executors;
using Pantheon.Framework.Flow;
using Pantheon.Framework.LlmClient;
using Pantheon.Framework.Storage;
using Pantheon.Framework.Template;

namespace SimpleSkill
{
    /// <summary>
    /// Input model for the summarization flow
    /// </summary>
    public class SummarizeInput
    {
        public string Text { get; set; } = "";
    }

    /// <summary>
    /// Output element model for the summarization flow
    /// </summary>
    public class SummarizeElement
    {
        public string Step { get; set; } = "";
    }

    /// <summary>
    /// Output result model for the summarization flow
    /// </summary>
    public class SummarizeResult
    {
        public string Summary { get; set; } = "";
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Set up logging
            // Set up a simple console logger
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
            
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Starting Simple Skill sample");

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

            // Create the in-memory executor
            var flows = new Dictionary<string, IFlow<object, object, object>>
            {
                { "summarize", (IFlow<object, object, object>)summarizeFlow }
            };
            var executor = new InMemoryExecutor(
                flowStorage, 
                flows, 
                loggerFactory.CreateLogger<InMemoryExecutor>());

            try
            {
                // Get input text
                Console.WriteLine("Enter text to summarize (or press Enter to use sample text):");
                string inputText = Console.ReadLine() ?? "";
                
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    inputText = "The quick brown fox jumps over the lazy dog. " +
                               "This sentence is often used because it contains every letter in the English alphabet. " +
                               "It's a pangram that has been used for typing practice, font display, and other purposes " +
                               "where all letters of the alphabet need to be visible.";
                    Console.WriteLine($"Using sample text: {inputText}");
                }

                // Create the input model
                var input = new SummarizeInput { Text = inputText };

                // Submit the flow
                Console.WriteLine("Submitting flow...");
                var flowRunId = await executor.SubmitFlowAsync("summarize", input, "user123");
                Console.WriteLine($"Flow run ID: {flowRunId}");

                // Wait for the flow to complete
                FlowRunStatus status;
                do
                {
                    await Task.Delay(100);
                    status = await executor.GetFlowStatusAsync(flowRunId);
                    Console.Write(".");
                } while (status != FlowRunStatus.Completed && status != FlowRunStatus.Failed);

                Console.WriteLine();

                // Get the elements
                var elements = await executor.GetFlowElementsAsync(flowRunId);
                Console.WriteLine("Elements:");
                foreach (var element in elements)
                {
                    if (element.Content is SummarizeElement summarizeElement)
                    {
                        Console.WriteLine($"  - {summarizeElement.Step}");
                    }
                }

                // Get the result
                var result = await executor.GetFlowResultAsync(flowRunId);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing flow");
            }

            // Wait for user input before exiting
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
