using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for language model clients
    /// </summary>
    public interface ILlmClient
    {
        /// <summary>
        /// The default model to use if none is specified
        /// </summary>
        string DefaultModel { get; }

        /// <summary>
        /// The current usage statistics for the client
        /// </summary>
        LlmUsage Usage { get; }

        /// <summary>
        /// Execute a completion request
        /// </summary>
        Task<LlmResponse> CompletionAsync(LlmCompletionRequest request);

        /// <summary>
        /// Execute a streaming completion request
        /// </summary>
        IAsyncEnumerable<object> StreamingCompletionAsync(LlmCompletionRequest request);

        /// <summary>
        /// Execute a chat completion request
        /// </summary>
        Task<LlmResponse> ChatCompletionAsync(LlmChatCompletionRequest request);

        /// <summary>
        /// Execute a streaming chat completion request
        /// </summary>
        IAsyncEnumerable<object> StreamingChatCompletionAsync(LlmChatCompletionRequest request);
    }
}
