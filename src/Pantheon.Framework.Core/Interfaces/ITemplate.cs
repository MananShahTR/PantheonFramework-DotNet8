using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for templates that can be rendered with input and parsed into output
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    /// <typeparam name="TOutput">The output type for the template</typeparam>
    public interface ITemplate<TInput, TOutput>
        where TInput : class
        where TOutput : class
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Render the template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered template as a string</returns>
        string Render(TInput input);

        /// <summary>
        /// Render the system template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered system template as a string, or null if no system template exists</returns>
        string? RenderSystemTemplate(TInput input);

        /// <summary>
        /// Parse the LLM response into the output type
        /// </summary>
        /// <param name="response">The LLM response to parse</param>
        /// <returns>The parsed response</returns>
        TOutput ParseResponse(string response);
    }

    /// <summary>
    /// Interface for templates that support streaming
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public interface IStreamingTemplate<TInput> where TInput : class
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Render the template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered template as a string</returns>
        string Render(TInput input);

        /// <summary>
        /// Render the system template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered system template as a string, or null if no system template exists</returns>
        string? RenderSystemTemplate(TInput input);
    }

    /// <summary>
    /// Interface for templates that use ChatML format and can be rendered with input and parsed into output
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    /// <typeparam name="TOutput">The output type for the template</typeparam>
    public interface IChatMlTemplate<TInput, TOutput>
        where TInput : class
        where TOutput : class
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Render the template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered template as a list of ChatML messages</returns>
        List<ChatMlMessage> Render(TInput input);

        /// <summary>
        /// Parse the LLM response into the output type
        /// </summary>
        /// <param name="response">The LLM response to parse</param>
        /// <returns>The parsed response</returns>
        TOutput ParseResponse(string response);
    }

    /// <summary>
    /// Interface for templates that use ChatML format and support streaming
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public interface IChatMlStreamingTemplate<TInput> where TInput : class
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Render the template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered template as a list of ChatML messages</returns>
        List<ChatMlMessage> Render(TInput input);
    }

    /// <summary>
    /// Interface for templates that use ChatML format with function calling capabilities
    /// </summary>
    /// <typeparam name="TInput">The input type for the template</typeparam>
    public interface IChatMlFunctionTemplate<TInput> where TInput : class
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Render the template with the provided input
        /// </summary>
        /// <param name="input">The input to render the template with</param>
        /// <returns>The rendered template as a list of ChatML messages</returns>
        List<ChatMlMessage> Render(TInput input);

        /// <summary>
        /// Parse the LLM response into a function call result
        /// </summary>
        /// <param name="response">The LLM response to parse</param>
        /// <returns>The parsed function call result</returns>
        FunctionCallResult ParseResponse(LlmResponse response);
    }

    /// <summary>
    /// Interface for a function that parses LLM responses into output objects
    /// </summary>
    /// <typeparam name="TOutput">The output type</typeparam>
    public interface IOutputParser<TOutput> where TOutput : class
    {
        /// <summary>
        /// Parse a response from the LLM into the output type
        /// </summary>
        /// <param name="response">The response from the LLM</param>
        /// <returns>The parsed output</returns>
        TOutput Parse(string response);
    }
}
