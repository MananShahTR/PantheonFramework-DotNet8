using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Executors
{
    /// <summary>
    /// An in-memory executor for flows
    /// </summary>
    public class InMemoryExecutor : IExecutor
    {
        private readonly IFlowStorage _flowStorage;
        private readonly Dictionary<string, IFlow<object, object, object>> _flows;
        private readonly ILogger<InMemoryExecutor>? _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;

        /// <summary>
        /// Creates a new in-memory executor
        /// </summary>
        /// <param name="flowStorage">The storage for flow runs</param>
        /// <param name="flows">The dictionary of flows to execute</param>
        /// <param name="logger">The logger to use</param>
        public InMemoryExecutor(
            IFlowStorage flowStorage,
            Dictionary<string, IFlow<object, object, object>> flows,
            ILogger<InMemoryExecutor>? logger = null)
        {
            _flowStorage = flowStorage;
            _flows = flows;
            _logger = logger;
            _cancellationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        }

        /// <inheritdoc />
        public async Task<Guid> SubmitFlowAsync<TInput>(string flowName, TInput input, string userId) where TInput : class
        {
            if (!_flows.TryGetValue(flowName, out var flow))
            {
                throw new ArgumentException($"Flow '{flowName}' not found", nameof(flowName));
            }

            var flowRun = new FlowRun(
                Guid.NewGuid(),
                flowName,
                userId,
                input);

            await _flowStorage.SaveFlowRunAsync(flowRun);
            
            // Create a cancellation token for this flow run
            var cts = new CancellationTokenSource();
            _cancellationTokens[flowRun.Id] = cts;

            // Execute the flow in the background
            _ = Task.Run(() => ExecuteFlowAsync(flowRun.Id, flow, input, cts.Token));

            return flowRun.Id;
        }

        /// <inheritdoc />
        public async Task<FlowRunStatus> GetFlowStatusAsync(Guid flowRunId)
        {
            var flowRun = await _flowStorage.GetFlowRunAsync(flowRunId);
            return flowRun?.Status ?? FlowRunStatus.Pending;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<FlowElement>> GetFlowElementsAsync(Guid flowRunId)
        {
            return _flowStorage.GetFlowElementsAsync(flowRunId);
        }

        /// <inheritdoc />
        public Task<object?> GetFlowResultAsync(Guid flowRunId)
        {
            return _flowStorage.GetFlowResultAsync(flowRunId);
        }

        /// <inheritdoc />
        public async Task<bool> CancelFlowAsync(Guid flowRunId)
        {
            if (_cancellationTokens.TryGetValue(flowRunId, out var cts))
            {
                cts.Cancel();
                await _flowStorage.UpdateFlowRunStatusAsync(flowRunId, FlowRunStatus.Canceled);
                return true;
            }

            return false;
        }

        private async Task ExecuteFlowAsync<TInput>(
            Guid flowRunId,
            IFlow<object, object, object> flow,
            TInput input,
            CancellationToken cancellationToken) where TInput : class
        {
            try
            {
                // Update flow status to running
                await _flowStorage.UpdateFlowRunStatusAsync(flowRunId, FlowRunStatus.Running);

                // Create a context for the flow
                var context = new FlowRunContext<object>();

                // Execute the flow and collect elements
                await foreach (var element in flow.RunAsync(input, context).WithCancellation(cancellationToken))
                {
                    // Save each element
                    var flowElement = new FlowElement(flowRunId, element);
                    await _flowStorage.SaveFlowElementAsync(flowElement);
                }

                // Get the result from the context
                object? contextResult = context.Result;

                // Update flow status to completed
                await _flowStorage.UpdateFlowRunStatusAsync(flowRunId, FlowRunStatus.Completed);

                // Save the result if there is one
                if (contextResult != null)
                {
                    await _flowStorage.SaveFlowResultAsync(flowRunId, contextResult);
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogInformation("Flow {FlowRunId} was canceled", flowRunId);
                await _flowStorage.UpdateFlowRunStatusAsync(flowRunId, FlowRunStatus.Canceled);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing flow {FlowRunId}", flowRunId);
                await _flowStorage.UpdateFlowRunStatusAsync(flowRunId, FlowRunStatus.Failed);
                await _flowStorage.UpdateFlowRunErrorMessageAsync(flowRunId, ex.Message);
            }
            finally
            {
                // Remove the cancellation token
                _cancellationTokens.TryRemove(flowRunId, out _);
            }
        }
    }
}
