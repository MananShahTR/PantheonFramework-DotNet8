using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using Pantheon.Framework.Flow;

namespace Pantheon.Framework.Api.Flows
{
    /// <summary>
    /// A simple echo flow that returns the input text after a delay
    /// </summary>
    public class EchoFlow : IFlow<object, object, object>
    {
        private readonly ILogger<EchoFlow> _logger;

        public EchoFlow(ILogger<EchoFlow> logger)
        {
            _logger = logger;
        }

        public string Name => "echo";
        public Type InputType => typeof(object);
        public Type ElementType => typeof(object);
        public Type ResultType => typeof(object);

        public async IAsyncEnumerable<object> RunAsync(object input, FlowRunContext<object> context)
        {
            // Log the input
            _logger.LogInformation("EchoFlow received input: {Input}", input?.ToString() ?? "null");

            // First element
            yield return new EchoElement { Message = "Starting echo flow" };

            // Add a delay to simulate processing time and demonstrate queue behavior
            await Task.Delay(2000);

            // Second element with progress
            yield return new EchoElement { Message = "Processing..." };

            // Another delay
            await Task.Delay(2000);

            // Convert the input to a result
            var result = new EchoResult { Text = input?.ToString() ?? "null" };
            
            // Set the result in the context
            context.SetResult(result);

            // Final element
            yield return new EchoElement { Message = "Echo complete" };
        }
    }

    /// <summary>
    /// A simple element for the echo flow
    /// </summary>
    public class EchoElement
    {
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// The result of the echo flow
    /// </summary>
    public class EchoResult
    {
        public string Text { get; set; } = string.Empty;
    }
}
