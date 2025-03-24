using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Template;
using Pantheon.Framework.LlmClient;

namespace ChatApplication
{
    /// <summary>
    /// Simple input model for the chat template
    /// </summary>
    public class ChatInput
    {
        public string UserMessage { get; set; } = "";
        public string SystemInstruction { get; set; } = "";
    }

    /// <summary>
    /// Simple output model for the chat template
    /// </summary>
    public class ChatOutput
    {
        public string Response { get; set; } = "";
    }

    /// <summary>
    /// Simple parser for the chat output
    /// </summary>
    public class ChatOutputParser : IOutputParser<ChatOutput>
    {
        public ChatOutput Parse(string response)
        {
            return new ChatOutput { Response = response };
        }
    }

    /// <summary>
    /// Sample program demonstrating chat capabilities
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Pantheon Framework Chat Application Sample");
            Console.WriteLine("===========================================");

            // Create a mock LLM client
            var llmClient = new MockLlmClient();
            var templateRunner = new TemplateRunner(llmClient);

            // Create a basic chat template
            var standardTemplate = new Template<ChatInput, ChatOutput>(
                name: "basic-template",
                templateContent: "User says: {{ user_message }}",
                outputParser: new ChatOutputParser(),
                systemTemplateContent: "{{ system_instruction }}"
            );

            // Create a ChatML template with multiple messages
            var chatMlTemplate = new ChatMlTemplate<ChatInput, ChatOutput>(
                name: "chatml-template",
                messageTemplates: new List<(string, string)>
                {
                    ("system", "{{ system_instruction }}"),
                    ("user", "{{ user_message }}")
                },
                outputParser: new ChatOutputParser()
            );

            // Create a ChatML function template
            var functionTemplate = new ChatMlFunctionTemplate<ChatInput>(
                name: "function-template",
                messageTemplates: new List<(string, string)>
                {
                    ("system", "{{ system_instruction }}"),
                    ("user", "{{ user_message }}")
                },
                functions: new List<LlmChatMlFunction>
                {
                    new LlmChatMlFunction(
                        "get_weather",
                        "Get the weather for a location",
                        new Dictionary<string, object>
                        {
                            ["location"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "The location to get weather for"
                            },
                            ["unit"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["enum"] = new[] { "celsius", "fahrenheit" }
                            }
                        }
                    )
                }
            );

            // Demo 1: Standard Template Completion
            Console.WriteLine("\n1. Standard Template Completion:");
            var standardInput = new ChatInput
            {
                UserMessage = "What is the capital of France?",
                SystemInstruction = "You are a helpful assistant."
            };

            var standardOutput = await templateRunner.ExecuteAsync(
                standardTemplate,
                standardInput,
                maxTokens: 100
            );
            Console.WriteLine($"Response: {standardOutput.Response}");
            Console.WriteLine($"Tokens used: {llmClient.Usage.TotalTokens}");

            // Demo 2: ChatML Template Completion
            Console.WriteLine("\n2. ChatML Template Completion:");
            var chatMlInput = new ChatInput
            {
                UserMessage = "What's the weather like today?",
                SystemInstruction = "You are a weather expert."
            };

            var chatMlOutput = await templateRunner.ExecuteChatAsync(
                chatMlTemplate,
                chatMlInput,
                maxTokens: 100
            );
            Console.WriteLine($"Response: {chatMlOutput.Response}");
            Console.WriteLine($"Tokens used: {llmClient.Usage.TotalTokens}");

            // Demo 3: ChatML Streaming Template
            Console.WriteLine("\n3. ChatML Streaming Template Completion:");
            var streamingTemplate = new ChatMlStreamingTemplate<ChatInput>(
                name: "streaming-template",
                messageTemplates: new List<(string, string)>
                {
                    ("system", "{{ system_instruction }}"),
                    ("user", "{{ user_message }}")
                }
            );

            Console.Write("Response: ");
            await foreach (var token in templateRunner.ExecuteStreamingChatAsync(
                streamingTemplate,
                chatMlInput,
                maxTokens: 100
            ))
            {
                Console.Write(token.Token);
            }
            Console.WriteLine($"\nTokens used: {llmClient.Usage.TotalTokens}");

            // Demo 4: Function Calling
            Console.WriteLine("\n4. Function Calling Template:");
            var functionInput = new ChatInput
            {
                UserMessage = "What's the weather in London?",
                SystemInstruction = "You are a weather assistant."
            };

            var functionResult = await templateRunner.ExecuteChatFunctionAsync(
                functionTemplate,
                functionInput,
                maxTokens: 100
            );
            
            Console.WriteLine($"Function called: {functionResult.Name}");
            Console.WriteLine($"Arguments: {functionResult.Arguments}");
            Console.WriteLine($"Tokens used: {llmClient.Usage.TotalTokens}");

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
