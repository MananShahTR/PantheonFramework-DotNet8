using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Storage
{
    /// <summary>
    /// In-memory implementation of flow storage
    /// </summary>
    public class InMemoryFlowStorage : IFlowStorage
    {
        private readonly ConcurrentDictionary<Guid, FlowRun> _flowRuns;
        private readonly ConcurrentDictionary<Guid, List<FlowElement>> _flowElements;
        private readonly ConcurrentDictionary<Guid, object> _flowResults;

        public InMemoryFlowStorage()
        {
            _flowRuns = new ConcurrentDictionary<Guid, FlowRun>();
            _flowElements = new ConcurrentDictionary<Guid, List<FlowElement>>();
            _flowResults = new ConcurrentDictionary<Guid, object>();
        }

        /// <inheritdoc />
        public Task<FlowRun?> GetFlowRunAsync(Guid id)
        {
            _flowRuns.TryGetValue(id, out var flowRun);
            return Task.FromResult(flowRun);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<FlowRun>> GetFlowRunsForUserAsync(string userId, int limit = 100)
        {
            var flowRuns = _flowRuns.Values
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .Take(limit)
                .ToList() as IReadOnlyList<FlowRun>;

            return Task.FromResult(flowRuns);
        }

        /// <inheritdoc />
        public Task<Guid> SaveFlowRunAsync(FlowRun flowRun)
        {
            _flowRuns[flowRun.Id] = flowRun;
            return Task.FromResult(flowRun.Id);
        }

        /// <inheritdoc />
        public Task UpdateFlowRunStatusAsync(Guid id, FlowRunStatus status)
        {
            if (_flowRuns.TryGetValue(id, out var flowRun))
            {
                flowRun.Status = status;
                
                if (status == FlowRunStatus.Completed || status == FlowRunStatus.Failed)
                {
                    flowRun.CompletedAt = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdateFlowRunCompletionTimeAsync(Guid id, DateTime completionTime)
        {
            if (_flowRuns.TryGetValue(id, out var flowRun))
            {
                flowRun.CompletedAt = completionTime;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task UpdateFlowRunErrorMessageAsync(Guid id, string errorMessage)
        {
            if (_flowRuns.TryGetValue(id, out var flowRun))
            {
                flowRun.ErrorMessage = errorMessage;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<Guid> SaveFlowElementAsync(FlowElement element)
        {
            var elements = _flowElements.GetOrAdd(element.FlowRunId, _ => new List<FlowElement>());
            
            lock (elements)
            {
                elements.Add(element);
            }

            return Task.FromResult(element.Id);
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<FlowElement>> GetFlowElementsAsync(Guid flowRunId)
        {
            if (_flowElements.TryGetValue(flowRunId, out var elements))
            {
                lock (elements)
                {
                    return Task.FromResult(elements.OrderBy(e => e.CreatedAt).ToList() as IReadOnlyList<FlowElement>);
                }
            }

            return Task.FromResult(new List<FlowElement>() as IReadOnlyList<FlowElement>);
        }

        /// <inheritdoc />
        public Task SaveFlowResultAsync(Guid flowRunId, object result)
        {
            _flowResults[flowRunId] = result;

            if (_flowRuns.TryGetValue(flowRunId, out var flowRun))
            {
                flowRun.Result = result;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<object?> GetFlowResultAsync(Guid flowRunId)
        {
            _flowResults.TryGetValue(flowRunId, out var result);
            return Task.FromResult(result);
        }
    }
}
