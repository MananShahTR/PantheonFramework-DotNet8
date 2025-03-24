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
    /// A very simple echo flow without delays to test flow processing
    /// </summary>
    public class SimpleEchoFlow : IFlow<object, object, object>
    {
        private readonly ILogger<SimpleEchoFlow> _logger;

        public SimpleEchoFlow(ILogger<SimpleEchoFlow> logger)
        {
            _logger = logger;
        }

        public string Name => "simple-echo";
        public Type InputType => typeof(object);
        public Type ElementType => typeof(object);
        public Type ResultType => typeof(object);

        public async IAsyncEnumerable<object> RunAsync(object input, FlowRunContext<object> context)
        {
            // Log the start
            _logger.LogInformation("SimpleEchoFlow started with input: {Input}", input?.ToString() ?? "null");
            
            // First element - just simple string
            yield return "Starting simple echo flow";
            
            // Second element
            yield return "Simple echo complete";
            
            // Set the result directly
            var result = new { Text = input?.ToString() ?? "null" };
            context.SetResult(result);
            
            _logger.LogInformation("SimpleEchoFlow completed successfully");
            
            // No delays or complex logic
        }
    }
}
