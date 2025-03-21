using System;
using System.Threading.Tasks;

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
