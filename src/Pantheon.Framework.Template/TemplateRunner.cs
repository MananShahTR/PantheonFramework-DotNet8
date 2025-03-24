using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Template
{
    /// <summary>
    /// A template runner that executes templates with an LLM client
    /// </summary>
    public class TemplateRunner
    {
        private readonly ILlmClient _llmClient;
        private readonly ILogger<TemplateRunner>? _logger;

        /// <summary>
        /// Creates a new template runner
        /// </summary>
        /// <param name="llmClient">The LLM client to use</param>
        /// <param name="logger">The logger to use</param>
        public TemplateRunner(ILlmClient llmClient, ILogger<TemplateRunner>? logger = null)
        {
            _llmClient = llmClient;
            _logger = logger;
        }

        /// <summary>
        /// Execute a template with the LLM client
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        /// <param name="template">The template to execute</param>
        /// <param name="input">The input for the template</param>
        /// <param name="maxTokens">The maximum number of tokens to generate</param>
        /// <param name="model">The model to use, or null to use the default</param>
        /// <param name="temperature">The temperature to use</param>
        /// <returns>The output of the template</returns>
        public async Task<TOutput> ExecuteAsync<TInput, TOutput>(
            ITemplate<TInput, TOutput> template,
            TInput input,
            int maxTokens,
            string? model = null,
            float temperature = 0.0f) 
            where TInput : class 
            where TOutput : class
        {
            _logger?.LogInformation("Executing template {TemplateName}", template.Name);

            // Render the template
            string renderedTemplate = template.Render(input);
            string? renderedSystemTemplate = template.RenderSystemTemplate(input);

            _logger?.LogDebug("Rendered template: {Template}", renderedTemplate);
            if (renderedSystemTemplate != null)
            {
                _logger?.LogDebug("Rendered system template: {SystemTemplate}", renderedSystemTemplate);
            }

            // Create the completion request
            var request = new LlmCompletionRequest(
                Prompt: renderedTemplate,
                MaxTokens: maxTokens,
                Model: model ?? _llmClient.DefaultModel,
                Temperature: temperature,
                Stop: template is Template<TInput, TOutput> t ? t.StopSequences : null,
                SystemInstruction: renderedSystemTemplate,
                SafetySettings: template is Template<TInput, TOutput> t2 ? t2.SafetySettings : null);

            // Execute the request
            LlmResponse response = await _llmClient.CompletionAsync(request);

            _logger?.LogInformation(
                "Template executed with {PromptTokens} prompt tokens and {CompletionTokens} completion tokens",
                response.Usage.PromptTokens,
                response.Usage.CompletionTokens);

            // Parse the response
            return template.ParseResponse(response.Result);
        }

        /// <summary>
        /// Execute a streaming template with the LLM client
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="template">The template to execute</param>
        /// <param name="input">The input for the template</param>
        /// <param name="maxTokens">The maximum number of tokens to generate</param>
        /// <param name="model">The model to use, or null to use the default</param>
        /// <param name="temperature">The temperature to use</param>
        /// <returns>A stream of tokens</returns>
        public async IAsyncEnumerable<StreamingToken> ExecuteStreamingAsync<TInput>(
            IStreamingTemplate<TInput> template,
            TInput input,
            int maxTokens,
            string? model = null,
            float temperature = 0.0f) 
            where TInput : class
        {
            _logger?.LogInformation("Executing streaming template {TemplateName}", template.Name);

            // Render the template
            string renderedTemplate = template.Render(input);
            string? renderedSystemTemplate = template.RenderSystemTemplate(input);

            _logger?.LogDebug("Rendered template: {Template}", renderedTemplate);
            if (renderedSystemTemplate != null)
            {
                _logger?.LogDebug("Rendered system template: {SystemTemplate}", renderedSystemTemplate);
            }

            // Create the completion request
            var request = new LlmCompletionRequest(
                Prompt: renderedTemplate,
                MaxTokens: maxTokens,
                Model: model ?? _llmClient.DefaultModel,
                Temperature: temperature,
                Stop: template is StreamingTemplate<TInput> t ? t.StopSequences : null,
                SystemInstruction: renderedSystemTemplate,
                SafetySettings: template is StreamingTemplate<TInput> t2 ? t2.SafetySettings : null);

            // Execute the request
            await foreach (var token in _llmClient.StreamingCompletionAsync(request))
            {
                if (token is StreamingToken streamingToken)
                {
                    yield return streamingToken;
                }
            }
        }

        /// <summary>
        /// Execute a ChatML template with the LLM client
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TOutput">The output type</typeparam>
        /// <param name="template">The template to execute</param>
        /// <param name="input">The input for the template</param>
        /// <param name="maxTokens">The maximum number of tokens to generate</param>
        /// <param name="model">The model to use, or null to use the default</param>
        /// <param name="temperature">The temperature to use</param>
        /// <param name="responseFormat">The response format to use</param>
        /// <returns>The output of the template</returns>
        public async Task<TOutput> ExecuteChatAsync<TInput, TOutput>(
            IChatMlTemplate<TInput, TOutput> template,
            TInput input,
            int maxTokens,
            string? model = null,
            float temperature = 0.0f,
            ResponseFormat? responseFormat = null) 
            where TInput : class 
            where TOutput : class
        {
            _logger?.LogInformation("Executing ChatML template {TemplateName}", template.Name);

            // Render the template
            List<ChatMlMessage> messages = template.Render(input);

            _logger?.LogDebug("Rendered {MessageCount} messages", messages.Count);

            // Create the chat completion request
            var request = new LlmChatCompletionRequest(
                Model: model ?? _llmClient.DefaultModel,
                Messages: messages,
                MaxTokens: maxTokens,
                Temperature: temperature,
                Stop: template is ChatMlTemplate<TInput, TOutput> t ? t.StopSequences : null,
                SafetySettings: template is ChatMlTemplate<TInput, TOutput> t2 ? t2.SafetySettings : null,
                ResponseFormat: responseFormat);

            // Execute the request
            LlmResponse response = await _llmClient.ChatCompletionAsync(request);

            _logger?.LogInformation(
                "ChatML template executed with {PromptTokens} prompt tokens and {CompletionTokens} completion tokens",
                response.Usage.PromptTokens,
                response.Usage.CompletionTokens);

            // Parse the response
            return template.ParseResponse(response.Result);
        }

        /// <summary>
        /// Execute a ChatML streaming template with the LLM client
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="template">The template to execute</param>
        /// <param name="input">The input for the template</param>
        /// <param name="maxTokens">The maximum number of tokens to generate</param>
        /// <param name="model">The model to use, or null to use the default</param>
        /// <param name="temperature">The temperature to use</param>
        /// <param name="responseFormat">The response format to use</param>
        /// <returns>A stream of tokens</returns>
        public async IAsyncEnumerable<StreamingToken> ExecuteStreamingChatAsync<TInput>(
            IChatMlStreamingTemplate<TInput> template,
            TInput input,
            int maxTokens,
            string? model = null,
            float temperature = 0.0f,
            ResponseFormat? responseFormat = null) 
            where TInput : class
        {
            _logger?.LogInformation("Executing streaming ChatML template {TemplateName}", template.Name);

            // Render the template
            List<ChatMlMessage> messages = template.Render(input);

            _logger?.LogDebug("Rendered {MessageCount} messages", messages.Count);

            // Create the chat completion request
            var request = new LlmChatCompletionRequest(
                Model: model ?? _llmClient.DefaultModel,
                Messages: messages,
                MaxTokens: maxTokens,
                Temperature: temperature,
                Stop: template is ChatMlStreamingTemplate<TInput> t ? t.StopSequences : null,
                SafetySettings: template is ChatMlStreamingTemplate<TInput> t2 ? t2.SafetySettings : null,
                ResponseFormat: responseFormat);

            // Execute the request
            await foreach (var token in _llmClient.StreamingChatCompletionAsync(request))
            {
                if (token is StreamingToken streamingToken)
                {
                    yield return streamingToken;
                }
            }
        }

        /// <summary>
        /// Execute a ChatML function template with the LLM client
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="template">The template to execute</param>
        /// <param name="input">The input for the template</param>
        /// <param name="maxTokens">The maximum number of tokens to generate</param>
        /// <param name="model">The model to use, or null to use the default</param>
        /// <param name="temperature">The temperature to use</param>
        /// <returns>The function call result</returns>
        public async Task<FunctionCallResult> ExecuteChatFunctionAsync<TInput>(
            IChatMlFunctionTemplate<TInput> template,
            TInput input,
            int maxTokens,
            string? model = null,
            float temperature = 0.0f) 
            where TInput : class
        {
            _logger?.LogInformation("Executing ChatML function template {TemplateName}", template.Name);

            // Render the template
            List<ChatMlMessage> messages = template.Render(input);

            _logger?.LogDebug("Rendered {MessageCount} messages", messages.Count);

            // Create the chat completion request with functions
            var request = new LlmChatCompletionRequest(
                Model: model ?? _llmClient.DefaultModel,
                Messages: messages,
                MaxTokens: maxTokens,
                Temperature: temperature,
                Stop: template is ChatMlFunctionTemplate<TInput> t ? t.StopSequences : null,
                SafetySettings: template is ChatMlFunctionTemplate<TInput> t2 ? t2.SafetySettings : null,
                Functions: template is ChatMlFunctionTemplate<TInput> t3 ? t3.Functions : null,
                FunctionCall: template is ChatMlFunctionTemplate<TInput> t4 ? t4.FunctionCall : null);

            // Execute the request
            LlmResponse response = await _llmClient.ChatCompletionAsync(request);

            _logger?.LogInformation(
                "ChatML function template executed with {PromptTokens} prompt tokens and {CompletionTokens} completion tokens",
                response.Usage.PromptTokens,
                response.Usage.CompletionTokens);

            // Parse the response
            return template.ParseResponse(response);
        }
    }
}
