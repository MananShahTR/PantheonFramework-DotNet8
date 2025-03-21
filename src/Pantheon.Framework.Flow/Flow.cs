using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Flow
{
    /// <summary>
    /// Implementation of a flow that can be executed with input to produce a stream of elements and a result
    /// </summary>
    /// <typeparam name="TInput">The input type for the flow</typeparam>
    /// <typeparam name="TElement">The element type produced during flow execution</typeparam>
    /// <typeparam name="TResult">The result type produced at the end of flow execution</typeparam>
    public class Flow<TInput, TElement, TResult> : IFlow<TInput, TElement, TResult> 
        where TInput : class 
        where TElement : class 
        where TResult : class
    {
        private readonly FlowFunction<TInput, TElement, TResult> _flowFunction;

        /// <summary>
        /// Creates a new Flow
        /// </summary>
        /// <param name="name">The name of the flow</param>
        /// <param name="flowFunction">The function to execute when the flow is run</param>
        /// <param name="inputType">The type of the input model</param>
        /// <param name="elementType">The type of the output element model</param>
        /// <param name="resultType">The type of the result model</param>
        public Flow(
            string name,
            FlowFunction<TInput, TElement, TResult> flowFunction,
            Type inputType,
            Type elementType,
            Type resultType)
        {
            Name = name;
            _flowFunction = flowFunction;
            InputType = inputType;
            ElementType = elementType;
            ResultType = resultType;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public Type InputType { get; }

        /// <inheritdoc />
        public Type ElementType { get; }

        /// <inheritdoc />
        public Type ResultType { get; }

        /// <inheritdoc />
        public async IAsyncEnumerable<TElement> RunAsync(TInput input, FlowRunContext<TResult> context)
        {
            await foreach (var element in _flowFunction(context, input))
            {
                yield return element;
            }
        }
    }

    /// <summary>
    /// Contains helper methods for creating flows
    /// </summary>
    public static class Flow
    {
        /// <summary>
        /// Creates a flow from a function
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TElement">The element type</typeparam>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="name">The name of the flow</param>
        /// <param name="inputType">The input type</param>
        /// <param name="elementType">The element type</param>
        /// <param name="resultType">The result type</param>
        /// <returns>A decorator function that creates a flow from a function</returns>
        public static Func<FlowFunction<TInput, TElement, TResult>, IFlow<TInput, TElement, TResult>> Create<TInput, TElement, TResult>(
            string name,
            Type inputType,
            Type elementType,
            Type resultType)
            where TInput : class
            where TElement : class
            where TResult : class
        {
            return (func) => new Flow<TInput, TElement, TResult>(
                name,
                func,
                inputType,
                elementType,
                resultType);
        }

        /// <summary>
        /// A decorator for creating flows
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TElement">The element type</typeparam>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="name">The name of the flow</param>
        /// <param name="inputModel">The input model type</param>
        /// <param name="outputElementModel">The output element model type</param>
        /// <param name="outputResultModel">The output result model type</param>
        /// <returns>A decorator function that creates a flow from a function</returns>
        public static Func<FlowFunction<TInput, TElement, TResult>, IFlow<TInput, TElement, TResult>> CreateFlow<TInput, TElement, TResult>(
            string name,
            Type inputModel,
            Type outputElementModel,
            Type outputResultModel)
            where TInput : class
            where TElement : class
            where TResult : class
        {
            return Create<TInput, TElement, TResult>(
                name,
                inputModel,
                outputElementModel,
                outputResultModel);
        }

        /// <summary>
        /// A record that represents an empty element model for simple flows
        /// </summary>
        public record EmptyElementModel();

        /// <summary>
        /// Creates a simple flow that doesn't produce elements
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="name">The name of the flow</param>
        /// <param name="inputModel">The input model type</param>
        /// <param name="outputResultModel">The output result model type</param>
        /// <returns>A decorator function that creates a flow from a function</returns>
        public static Func<FlowSimpleFunction<TInput, TResult>, IFlow<TInput, EmptyElementModel, TResult>> CreateSimpleFlow<TInput, TResult>(
            string name,
            Type inputModel,
            Type outputResultModel)
            where TInput : class
            where TResult : class
        {
            return (func) =>
            {
                async IAsyncEnumerable<EmptyElementModel> Wrapper(FlowRunContext<TResult> context, TInput input)
                {
                    await func(context, input);
                    yield return new EmptyElementModel();
                }

                return new Flow<TInput, EmptyElementModel, TResult>(
                    name,
                    Wrapper,
                    inputModel,
                    typeof(EmptyElementModel),
                    outputResultModel);
            };
        }
    }
}
