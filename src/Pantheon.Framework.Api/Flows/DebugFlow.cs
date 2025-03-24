using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;
using System.Text.Json;

namespace Pantheon.Framework.Api.Flows
{
    /// <summary>
    /// A minimal debug flow with extensive logging to diagnose execution issues
    /// </summary>
    public class DebugFlow : IFlow<object, object, object>
    {
        private readonly ILogger<DebugFlow> _logger;

        public DebugFlow(ILogger<DebugFlow> logger)
        {
            _logger = logger;
        }

        public string Name => "debug";
        public Type InputType => typeof(object);
        public Type ElementType => typeof(object);
        public Type ResultType => typeof(object);

        public async IAsyncEnumerable<object> RunAsync(object input, FlowRunContext<object> context)
        {
            _logger.LogInformation("DebugFlow started with input type: {InputType}", 
                input?.GetType().FullName ?? "null");

            try
            {
                // Log the input details
                if (input == null)
                {
                    _logger.LogWarning("DebugFlow received null input");
                }
                else if (input is JsonElement jsonElement)
                {
                    _logger.LogInformation("Input is JsonElement with ValueKind: {ValueKind}", 
                        jsonElement.ValueKind);
                    
                    if (jsonElement.ValueKind == JsonValueKind.String)
                    {
                        string value = jsonElement.GetString() ?? "null";
                        _logger.LogInformation("JsonElement string value: {Value}", value);
                    }
                }
                else
                {
                    _logger.LogInformation("Input toString: {InputString}", input.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception examining input: {Message}", ex.Message);
            }
            
            // First element
            string element1 = "Debug flow started";
            _logger.LogInformation("Yielding element: {Element}", element1);
            yield return element1;
            
            try
            {
                // Small delay (shorter than original echo flow)
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during delay: {Message}", ex.Message);
                throw;
            }
            
            // Second element
            string element2 = "Debug flow processing";
            _logger.LogInformation("Yielding element: {Element}", element2);
            yield return element2;
            
            try
            {
                // Set result
                string result = $"Debug flow result: {input?.ToString() ?? "null"}";
                _logger.LogInformation("Setting result: {Result}", result);
                context.SetResult(result as object);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception setting result: {Message}", ex.Message);
                throw;
            }
            
            // Final element
            string element3 = "Debug flow completed";
            _logger.LogInformation("Yielding element: {Element}", element3);
            yield return element3;
            
            _logger.LogInformation("DebugFlow completed successfully");
        }
    }
}
