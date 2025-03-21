using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for flows that can be executed with input to produce a stream of elements and a result
    /// </summary>
    /// <typeparam name="TInput">The input type for the flow</typeparam>
    /// <typeparam name="TElement">The element type produced during flow execution</typeparam>
    /// <typeparam name="TResult">The result type produced at the end of flow execution</typeparam>
    public interface IFlow<TInput, TElement, TResult>
        where TInput : class
        where TElement : class
        where TResult : class
    {
        /// <summary>
        /// Name of the flow
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Type of the input model
        /// </summary>
        Type InputType { get; }

        /// <summary>
        /// Type of the element model
        /// </summary>
        Type ElementType { get; }

        /// <summary>
        /// Type of the result model
        /// </summary>
        Type ResultType { get; }

        /// <summary>
        /// Run the flow with the provided input and context
        /// </summary>
        /// <param name="input">The input for the flow</param>
        /// <param name="context">The context for the flow run</param>
        /// <returns>An async enumerable of elements produced during flow execution</returns>
        IAsyncEnumerable<TElement> RunAsync(TInput input, FlowRunContext<TResult> context);
    }

    /// <summary>
    /// Delegate type for flow functions
    /// </summary>
    /// <typeparam name="TInput">The input type for the flow</typeparam>
    /// <typeparam name="TElement">The element type produced during flow execution</typeparam>
    /// <typeparam name="TResult">The result type produced at the end of flow execution</typeparam>
    /// <param name="context">The context for the flow run</param>
    /// <param name="input">The input for the flow</param>
    /// <returns>An async enumerable of elements produced during flow execution</returns>
    public delegate IAsyncEnumerable<TElement> FlowFunction<TInput, TElement, TResult>(
        FlowRunContext<TResult> context,
        TInput input)
        where TInput : class
        where TElement : class
        where TResult : class;

    /// <summary>
    /// Delegate type for simple flow functions that don't produce elements
    /// </summary>
    /// <typeparam name="TInput">The input type for the flow</typeparam>
    /// <typeparam name="TResult">The result type produced at the end of flow execution</typeparam>
    /// <param name="context">The context for the flow run</param>
    /// <param name="input">The input for the flow</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public delegate Task FlowSimpleFunction<TInput, TResult>(
        FlowRunContext<TResult> context,
        TInput input)
        where TInput : class
        where TResult : class;
}
