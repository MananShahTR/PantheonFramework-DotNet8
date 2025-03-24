using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Executors
{
    /// <summary>
    /// A flow executor that uses a queue system for managing pending and in-progress flows.
    /// </summary>
    public class QueuedExecutor : IExecutor
    {
        private readonly IFlowStorage _flowStorage;
        private readonly IFlowQueue _flowQueue;
        private readonly Dictionary<string, IFlow<object, object, object>> _flows;
        private readonly ILogger<QueuedExecutor>? _logger;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokens;
        private readonly SemaphoreSlim _executionSemaphore;
        private readonly int _maxConcurrentFlows;
        private readonly CancellationTokenSource _shutdownCts = new();
        private Task? _queueProcessorTask;
        private bool _isProcessing;

        /// <summary>
        /// Creates a new queued executor
        /// </summary>
        /// <param name="flowStorage">The storage for flow runs</param>
        /// <param name="flowQueue">The queue for flow management</param>
        /// <param name="flows">The dictionary of flows to execute</param>
        /// <param name="maxConcurrentFlows">Maximum number of flows to execute concurrently</param>
        /// <param name="logger">The logger to use</param>
        public QueuedExecutor(
            IFlowStorage flowStorage,
            IFlowQueue flowQueue,
            Dictionary<string, IFlow<object, object, object>> flows,
            int maxConcurrentFlows = 5,
            ILogger<QueuedExecutor>? logger = null)
        {
            _flowStorage = flowStorage;
            _flowQueue = flowQueue;
            _flows = flows;
            _logger = logger;
            _maxConcurrentFlows = maxConcurrentFlows;
            _executionSemaphore = new SemaphoreSlim(maxConcurrentFlows, maxConcurrentFlows);
            _cancellationTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            
            StartQueueProcessor();
        }

        /// <summary>
        /// Starts the queue processor task
        /// </summary>
        private void StartQueueProcessor()
        {
            if (_queueProcessorTask != null)
            {
                return;
            }

            _isProcessing = true;
            _queueProcessorTask = Task.Run(() => ProcessQueueAsync(_shutdownCts.Token));
        }

        /// <summary>
        /// Stops the queue processor task
        /// </summary>
        public async Task StopAsync()
        {
            _isProcessing = false;
            _shutdownCts.Cancel();
            
            if (_queueProcessorTask != null)
            {
                await _queueProcessorTask;
                _queueProcessorTask = null;
            }
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
            
            // Add the flow to the pending queue
            await _flowQueue.PushToPendingAsync(flowRun.Id);

            // Ensure the queue processor is running
            if (!_isProcessing)
            {
                StartQueueProcessor();
            }

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

        /// <summary>
        /// Process the flow queue continuously
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Queue processor started");

            try
            {
                while (_isProcessing && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Log queue state
                        _logger?.LogDebug("Queue processor checking for flows. Semaphore count: {SemaphoreCount}", _executionSemaphore.CurrentCount);
                        
                        // Requeue any expired flows
                        await _flowQueue.RequeueExpiredFlowsAsync();

                        // Only try to get a new flow if we have capacity to process it
                        if (_executionSemaphore.CurrentCount > 0)
                        {
                            // Get the next flow from the queue
                            var flowId = await _flowQueue.PopFromPendingAsync();
                            if (flowId.HasValue)
                            {
                                _logger?.LogInformation("Starting processing of flow: {FlowId}", flowId.Value);
                                // Simple, safer approach using Task.Run without Wait
                                try 
                                {
                                    _logger?.LogInformation("Starting processing of flow {FlowId} in background", flowId.Value);
                                    
                                    // Use Task.Run with proper error handling
                                    Task.Run(async () => 
                                    {
                                        try 
                                        {
                                            await ProcessFlowAsync(flowId.Value);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger?.LogError(ex, "Error processing flow {FlowId} in background task", flowId.Value);
                                        }
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogError(ex, "Exception starting flow task: {FlowId}", flowId.Value);
                                }
                            }
                            else
                            {
                                _logger?.LogDebug("No flows found in pending queue");
                            }
                        }
                        else
                        {
                            _logger?.LogDebug("No capacity to process more flows");
                        }

                        // Wait a short time before checking the queue again
                        await Task.Delay(100, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // Normal cancellation, just exit the loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error processing queue");
                        await Task.Delay(1000, cancellationToken); // Wait longer after an error
                    }
                }
            }
            finally
            {
                _logger?.LogInformation("Queue processor stopped");
            }
        }

        /// <summary>
        /// Process a single flow
        /// </summary>
        private async Task ProcessFlowAsync(Guid flowId)
        {
            _logger?.LogInformation("Starting to process flow {FlowId}", flowId);
            
            // Wait until we have capacity to execute this flow
            await _executionSemaphore.WaitAsync();
            _logger?.LogDebug("Acquired execution semaphore for flow {FlowId}", flowId);

            try
            {
                // Get the flow run from storage
                _logger?.LogDebug("Getting flow run details for {FlowId}", flowId);
                var flowRun = await _flowStorage.GetFlowRunAsync(flowId);
                if (flowRun == null)
                {
                    _logger?.LogWarning("Flow {FlowId} not found in storage", flowId);
                    return;
                }
                
                if (flowRun.Status != FlowRunStatus.Pending)
                {
                    _logger?.LogWarning("Flow {FlowId} is not in pending state, current status: {Status}", flowId, flowRun.Status);
                    return;
                }
                
                _logger?.LogDebug("Flow {FlowId} found with name {FlowName}", flowId, flowRun.FlowName);

                // Get the flow from the registry
                _logger?.LogDebug("Looking up flow implementation for {FlowName}", flowRun.FlowName);
                if (!_flows.TryGetValue(flowRun.FlowName, out var flow))
                {
                    _logger?.LogError("Flow type {FlowName} not found for flow {FlowId}", flowRun.FlowName, flowId);
                    await _flowStorage.UpdateFlowRunStatusAsync(flowId, FlowRunStatus.Failed);
                    await _flowStorage.UpdateFlowRunErrorMessageAsync(flowId, $"Flow type '{flowRun.FlowName}' not found");
                    return;
                }
                
                _logger?.LogDebug("Flow implementation found for {FlowName}", flowRun.FlowName);

                // Create a cancellation token for this flow
                _logger?.LogDebug("Creating cancellation token for flow {FlowId}", flowId);
                var cts = new CancellationTokenSource();
                _cancellationTokens[flowId] = cts;

                // Reset the flow time in the queue to prevent it from being requeued
                _logger?.LogDebug("Resetting flow time for {FlowId} to prevent requeuing", flowId);
                await _flowQueue.ResetFlowTimeAsync(flowId);

                try
                {
                    // Update flow status to running
                    _logger?.LogInformation("Updating flow {FlowId} status to Running", flowId);
                    await _flowStorage.UpdateFlowRunStatusAsync(flowId, FlowRunStatus.Running);

                    // Create a context for the flow
                    _logger?.LogDebug("Creating context for flow {FlowId}", flowId);
                    var context = new FlowRunContext<object>();

                    // Log the input type
                    _logger?.LogDebug("Flow {FlowId} input type: {InputType}", flowId, 
                        flowRun.Input?.GetType().FullName ?? "null");

                    // Execute the flow and collect elements
                    _logger?.LogInformation("Starting execution of flow {FlowId} with implementation {FlowName}", 
                        flowId, flowRun.FlowName);
                        
                    await foreach (var element in flow.RunAsync(flowRun.Input!, context).WithCancellation(cts.Token))
                    {
                        _logger?.LogDebug("Flow {FlowId} produced element: {ElementType}", 
                            flowId, element?.GetType().Name ?? "null");
                            
                        // Save each element
                        var flowElement = new FlowElement(flowId, element);
                        await _flowStorage.SaveFlowElementAsync(flowElement);
                        
                        // Reset the flow time in the queue to prevent it from being requeued
                        await _flowQueue.ResetFlowTimeAsync(flowId);
                    }

                    // Get the result from the context
                    _logger?.LogDebug("Getting result from context for flow {FlowId}", flowId);
                    object? contextResult = context.Result;

                    // Update flow status to completed
                    _logger?.LogInformation("Updating flow {FlowId} status to Completed", flowId);
                    await _flowStorage.UpdateFlowRunStatusAsync(flowId, FlowRunStatus.Completed);

                    // Save the result if there is one
                    if (contextResult != null)
                    {
                        _logger?.LogDebug("Saving result for flow {FlowId}", flowId);
                        await _flowStorage.SaveFlowResultAsync(flowId, contextResult);
                    }
                    else
                    {
                        _logger?.LogDebug("No result to save for flow {FlowId}", flowId);
                    }

                    // Remove the flow from the in-progress queue
                    _logger?.LogDebug("Removing flow {FlowId} from in-progress queue", flowId);
                    await _flowQueue.PopFromInProgressAsync(flowId);
                    
                    _logger?.LogInformation("Flow {FlowId} execution completed successfully", flowId);
                }
                catch (OperationCanceledException)
                {
                    _logger?.LogInformation("Flow {FlowId} was canceled", flowId);
                    await _flowStorage.UpdateFlowRunStatusAsync(flowId, FlowRunStatus.Canceled);
                    await _flowQueue.PopFromInProgressAsync(flowId);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing flow {FlowId}", flowId);
                    await _flowStorage.UpdateFlowRunStatusAsync(flowId, FlowRunStatus.Failed);
                    await _flowStorage.UpdateFlowRunErrorMessageAsync(flowId, ex.Message);
                    await _flowQueue.PopFromInProgressAsync(flowId);
                }
                finally
                {
                    // Remove the cancellation token
                    _cancellationTokens.TryRemove(flowId, out _);
                }
            }
            finally
            {
                _executionSemaphore.Release();
            }
        }
    }
}
