using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;

namespace Pantheon.Framework.FlowQueue
{
    /// <summary>
    /// In-memory implementation of IFlowQueue.
    /// 
    /// This class provides a simple in-memory implementation of the flow queue interface,
    /// managing flows in pending and in-progress states with support for visibility timeouts
    /// and flow requeuing.
    /// </summary>
    public class InMemoryFlowQueue : IFlowQueue
    {
        private Queue<Guid> _pendingQueue = new();
        private Queue<Guid> _inProgressQueue = new();
        private readonly Dictionary<Guid, DateTime> _inProgressTimestamps = new();
        private readonly TimeSpan _visibilityTimeout;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly ILogger<InMemoryFlowQueue>? _logger;

        /// <summary>
        /// Initializes a new instance of the InMemoryFlowQueue class.
        /// </summary>
        /// <param name="visibilityTimeoutSeconds">Timeout in seconds after which in-progress flows are considered expired</param>
        /// <param name="logger">Optional logger for diagnostic information</param>
        public InMemoryFlowQueue(int visibilityTimeoutSeconds = 30, ILogger<InMemoryFlowQueue>? logger = null)
        {
            _visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutSeconds);
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task PushToPendingAsync(Guid flowId)
        {
            await _lock.WaitAsync();
            try
            {
                _pendingQueue.Enqueue(flowId);
                _logger?.LogInformation("Flow {FlowId} pushed to pending queue. Pending count: {PendingCount}", 
                    flowId, _pendingQueue.Count);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<Guid?> PopFromPendingAsync()
        {
            await _lock.WaitAsync();
            try
            {
                if (_pendingQueue.Count == 0)
                {
                    _logger?.LogDebug("Pending queue is empty, no flow to pop");
                    return null;
                }

                var flowId = _pendingQueue.Dequeue();
                _logger?.LogInformation("Flow {FlowId} popped from pending queue. Remaining pending: {PendingCount}", 
                    flowId, _pendingQueue.Count);
                
                await PushToInProgressAsync(flowId);
                return flowId;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task PushToInProgressAsync(Guid flowId)
        {
            await _lock.WaitAsync();
            try
            {
                _inProgressQueue.Enqueue(flowId);
                _inProgressTimestamps[flowId] = DateTime.UtcNow;
                _logger?.LogInformation("Flow {FlowId} pushed to in-progress queue. In-progress count: {InProgressCount}", 
                    flowId, _inProgressQueue.Count);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task PopFromInProgressAsync(Guid flowId)
        {
            await _lock.WaitAsync();
            try
            {
                if (_inProgressTimestamps.ContainsKey(flowId))
                {
                    // Remove the flow ID from the in-progress queue
                    var tempQueue = new Queue<Guid>();
                    while (_inProgressQueue.Count > 0)
                    {
                        var id = _inProgressQueue.Dequeue();
                        if (id != flowId)
                        {
                            tempQueue.Enqueue(id);
                        }
                    }
                    
                    // Restore the queue without the removed flow ID
                    while (tempQueue.Count > 0)
                    {
                        _inProgressQueue.Enqueue(tempQueue.Dequeue());
                    }
                    
                    _inProgressTimestamps.Remove(flowId);
                    _logger?.LogInformation("Flow {FlowId} removed from in-progress queue. Remaining in-progress: {InProgressCount}", 
                        flowId, _inProgressQueue.Count);
                }
                else
                {
                    _logger?.LogWarning("Attempted to remove flow {FlowId} from in-progress queue, but it was not found", flowId);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task ResetFlowTimeAsync(Guid flowId)
        {
            await _lock.WaitAsync();
            try
            {
                if (_inProgressTimestamps.ContainsKey(flowId))
                {
                    _inProgressTimestamps[flowId] = DateTime.UtcNow;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RequeueExpiredFlowsAsync()
        {
            await _lock.WaitAsync();
            try
            {
                var expiredFlows = await CheckExpiredFlowsAsync();
                foreach (var flowId in expiredFlows)
                {
                    await RequeueFlowAsync(flowId);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Checks for flows that have exceeded their visibility timeout.
        /// </summary>
        /// <returns>A list of expired flow IDs</returns>
        private Task<List<Guid>> CheckExpiredFlowsAsync()
        {
            var currentTime = DateTime.UtcNow;
            var expiredFlows = new List<Guid>();

            foreach (var flowId in _inProgressQueue)
            {
                if (_inProgressTimestamps.TryGetValue(flowId, out var timestamp))
                {
                    if (currentTime - timestamp > _visibilityTimeout)
                    {
                        expiredFlows.Add(flowId);
                    }
                }
            }

            return Task.FromResult(expiredFlows);
        }

        /// <summary>
        /// Moves a flow from the in-progress queue back to the pending queue.
        /// </summary>
        /// <param name="flowId">The ID of the flow to requeue</param>
        private Task RequeueFlowAsync(Guid flowId)
        {
            if (_inProgressTimestamps.ContainsKey(flowId))
            {
                _pendingQueue.Enqueue(flowId);
                
                // Remove the flow ID from the in-progress queue
                var tempQueue = new Queue<Guid>();
                while (_inProgressQueue.Count > 0)
                {
                    var id = _inProgressQueue.Dequeue();
                    if (id != flowId)
                    {
                        tempQueue.Enqueue(id);
                    }
                }
                
                // Restore the queue without the removed flow ID
                while (tempQueue.Count > 0)
                {
                    _inProgressQueue.Enqueue(tempQueue.Dequeue());
                }
                
                _inProgressTimestamps.Remove(flowId);
            }

            return Task.CompletedTask;
        }
    }
}
