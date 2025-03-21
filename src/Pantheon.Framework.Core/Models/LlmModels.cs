using System;
using System.Collections.Generic;

namespace Pantheon.Framework.Core.Models
{
    /// <summary>
    /// Represents usage statistics for an LLM request
    /// </summary>
    public record LlmUsage(int PromptTokens, int CompletionTokens)
    {
        public int TotalTokens => PromptTokens + CompletionTokens;
    }

    /// <summary>
    /// Represents a response from an LLM
    /// </summary>
    public record LlmResponse(string Result, string FinishReason, LlmUsage Usage, object? Logprobs = null);

    /// <summary>
    /// Represents a streaming token from an LLM
    /// </summary>
    public record StreamingToken(string Token, string? FinishReason);

    /// <summary>
    /// Represents a safety setting for LLM requests
    /// </summary>
    public record SafetySetting(HarmCategory Category, HarmBlockThreshold Threshold);

    /// <summary>
    /// Harm categories for safety settings
    /// </summary>
    public enum HarmCategory
    {
        Unspecified = 0,
        Derogatory = 1,
        Toxicity = 2,
        Violence = 3,
        Sexual = 4,
        Medical = 5,
        Dangerous = 6,
        Harassment = 7,
        HateSpeech = 8,
        SexuallyExplicit = 9,
        DangerousContent = 10
    }

    /// <summary>
    /// Thresholds for blocking content based on harm categories
    /// </summary>
    public enum HarmBlockThreshold
    {
        Unspecified = 0,
        BlockLowAndAbove = 1,
        BlockMediumAndAbove = 2,
        BlockOnlyHigh = 3,
        BlockNone = 4
    }

    /// <summary>
    /// Model for LLM completion requests
    /// </summary>
    public record LlmCompletionRequest(
        string Prompt,
        int MaxTokens,
        string Model,
        float Temperature = 0.0f,
        List<string>? Stop = null,
        int? Logprobs = null,
        float TopP = 1.0f,
        string User = "backlog",
        string? SystemInstruction = null,
        List<SafetySetting>? SafetySettings = null,
        string? ResponseMimeType = null);

    /// <summary>
    /// Message for chat completion requests
    /// </summary>
    public record ChatMlMessage(string Role, string Content);

    /// <summary>
    /// Function for chat completion requests
    /// </summary>
    public record LlmChatMlFunction(string Name, string Description, Dictionary<string, object>? Parameters);

    /// <summary>
    /// Model for LLM chat completion requests
    /// </summary>
    public record LlmChatCompletionRequest(
        string Model,
        List<ChatMlMessage> Messages,
        int MaxTokens,
        List<LlmChatMlFunction>? Functions = null,
        string? FunctionCall = null,
        ResponseFormat? ResponseFormat = null,
        string User = "backlog",
        List<string>? Stop = null,
        float Temperature = 0.0f,
        bool Logprobs = false,
        int? TopLogprobs = null,
        string? SystemInstruction = null,
        List<SafetySetting>? SafetySettings = null,
        float TopP = 1.0f,
        string? CachedContent = null);

    /// <summary>
    /// Format for LLM responses
    /// </summary>
    public record ResponseFormat(string Type, Dictionary<string, object>? JsonSchema = null);
}
