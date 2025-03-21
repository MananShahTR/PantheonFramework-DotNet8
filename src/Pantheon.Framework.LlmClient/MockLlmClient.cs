using System;
using System.Collections.Generic;
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
        public static string DefaultChatCompletionResponseFunc(LlmChatCompletionRequest request) =>
            $"This is a mock response to a chat with {request.Messages.Count} messages";

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
