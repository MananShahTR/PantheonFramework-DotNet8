using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.LlmClient
{
    /// <summary>
    /// A mock LLM client for testing
    /// </summary>
    public class MockLlmClient : ILlmClient
    {
        private readonly ILogger<MockLlmClient>? _logger;
        private LlmUsage _usage = new LlmUsage(0, 0);

        /// <summary>
        /// Creates a new mock LLM client
        /// </summary>
        /// <param name="logger">The logger to use</param>
        public MockLlmClient(ILogger<MockLlmClient>? logger = null)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string DefaultModel => "mock-model";

        /// <inheritdoc />
        public LlmUsage Usage => _usage;

        /// <summary>
        /// Function to determine the response for a given completion request
        /// </summary>
        public Func<LlmCompletionRequest, string> CompletionResponseFunc { get; set; } = DefaultCompletionResponseFunc;

        /// <summary>
        /// Function to determine the response for a given chat completion request
        /// </summary>
        public Func<LlmChatCompletionRequest, string> ChatCompletionResponseFunc { get; set; } = DefaultChatCompletionResponseFunc;

        /// <summary>
        /// Default function to determine the response for a completion request
        /// </summary>
        public static string DefaultCompletionResponseFunc(LlmCompletionRequest request) =>
            $"This is a mock response to: {request.Prompt}";

        /// <summary>
        /// Default function to determine the response for a chat completion request
        /// </summary>
        public static string DefaultChatCompletionResponseFunc(LlmChatCompletionRequest request)
        {
            // If functions are provided, generate a function call response
            if (request.Functions != null && request.Functions.Count > 0)
            {
                var function = request.Functions[0];
                
                // If a specific function is requested, use that instead
                if (!string.IsNullOrEmpty(request.FunctionCall) && 
                    request.Functions.Exists(f => f.Name == request.FunctionCall))
                {
                    function = request.Functions.Find(f => f.Name == request.FunctionCall);
                }
                
                return GenerateFunctionCallResponse(function);
            }

            // If response format is specified as JSON, return a JSON structure
            if (request.ResponseFormat?.Type == "json")
            {
                return "{\"message\": \"This is a mock JSON response\", \"count\": 42}";
            }

            return $"This is a mock response to a chat with {request.Messages.Count} messages";
        }

        /// <summary>
        /// Generate a function call response for the specified function
        /// </summary>
        private static string GenerateFunctionCallResponse(LlmChatMlFunction? function)
        {
            // Handle null function gracefully
            if (function == null)
            {
                return "function_call: unknown\narguments: {}";
            }
            
            // Create a simple mock response for the function
            var mockArgs = new Dictionary<string, object>();
            
            // Add some default arguments based on the function parameters
            if (function.Parameters != null)
            {
                foreach (var param in function.Parameters)
                {
                    // Add a mock value based on the parameter name
                    mockArgs[param.Key] = $"mock-value-for-{param.Key}";
                }
            }

            string args = JsonSerializer.Serialize(mockArgs);
            return $"function_call: {function.Name}\narguments: {args}";
        }

        /// <inheritdoc />
        public Task<LlmResponse> CompletionAsync(LlmCompletionRequest request)
        {
            _logger?.LogInformation("Executing completion request with prompt: {Prompt}", request.Prompt);

            // Calculate token counts
            int promptTokens = CountTokens(request.Prompt);
            string response = CompletionResponseFunc(request);
            int completionTokens = CountTokens(response);

            // Update usage
            _usage = new LlmUsage(_usage.PromptTokens + promptTokens, _usage.CompletionTokens + completionTokens);

            return Task.FromResult(new LlmResponse(
                Result: response,
                FinishReason: "stop",
                Usage: new LlmUsage(promptTokens, completionTokens)
            ));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<object> StreamingCompletionAsync(LlmCompletionRequest request)
        {
            _logger?.LogInformation("Executing streaming completion request with prompt: {Prompt}", request.Prompt);

            // Calculate token counts
            int promptTokens = CountTokens(request.Prompt);
            string response = CompletionResponseFunc(request);
            int completionTokens = CountTokens(response);

            // Update usage
            var usage = new LlmUsage(promptTokens, completionTokens);
            _usage = new LlmUsage(_usage.PromptTokens + promptTokens, _usage.CompletionTokens + completionTokens);

            // First yield the usage
            yield return usage;

            // Then split the response into tokens and yield them
            string[] tokens = SplitIntoTokens(response);
            for (int i = 0; i < tokens.Length; i++)
            {
                // Simulate some delay
                await Task.Delay(10);

                bool isLast = i == tokens.Length - 1;
                yield return new StreamingToken(
                    Token: tokens[i],
                    FinishReason: isLast ? "stop" : null
                );
            }
        }

        /// <inheritdoc />
        public Task<LlmResponse> ChatCompletionAsync(LlmChatCompletionRequest request)
        {
            _logger?.LogInformation("Executing chat completion request with {MessageCount} messages", request.Messages.Count);

            // Calculate token counts
            int promptTokens = CountChatTokens(request.Messages);
            string response = ChatCompletionResponseFunc(request);
            int completionTokens = CountTokens(response);

            // Update usage
            _usage = new LlmUsage(_usage.PromptTokens + promptTokens, _usage.CompletionTokens + completionTokens);

            return Task.FromResult(new LlmResponse(
                Result: response,
                FinishReason: "stop",
                Usage: new LlmUsage(promptTokens, completionTokens)
            ));
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<object> StreamingChatCompletionAsync(LlmChatCompletionRequest request)
        {
            _logger?.LogInformation("Executing streaming chat completion request with {MessageCount} messages", request.Messages.Count);

            // Calculate token counts
            int promptTokens = CountChatTokens(request.Messages);
            string response = ChatCompletionResponseFunc(request);
            int completionTokens = CountTokens(response);

            // Update usage
            var usage = new LlmUsage(promptTokens, completionTokens);
            _usage = new LlmUsage(_usage.PromptTokens + promptTokens, _usage.CompletionTokens + completionTokens);

            // First yield the usage
            yield return usage;

            // Then split the response into tokens and yield them
            string[] tokens = SplitIntoTokens(response);
            for (int i = 0; i < tokens.Length; i++)
            {
                // Simulate some delay
                await Task.Delay(10);

                bool isLast = i == tokens.Length - 1;
                yield return new StreamingToken(
                    Token: tokens[i],
                    FinishReason: isLast ? "stop" : null
                );
            }
        }

        /// <summary>
        /// Simple method to count tokens in a string
        /// </summary>
        private int CountTokens(string text)
        {
            // This is a very naive implementation, but it's good enough for a mock
            return text.Split(' ').Length;
        }

        /// <summary>
        /// Simple method to count tokens in chat messages
        /// </summary>
        private int CountChatTokens(List<ChatMlMessage> messages)
        {
            int count = 0;
            foreach (var message in messages)
            {
                count += CountTokens(message.Content);
            }
            return count;
        }

        /// <summary>
        /// Split a string into tokens
        /// </summary>
        private string[] SplitIntoTokens(string text)
        {
            // This is a very naive implementation, but it's good enough for a mock
            return text.Split(' ');
        }
    }
}
