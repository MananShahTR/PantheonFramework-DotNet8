using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for flow storage implementations
    /// </summary>
    public interface IFlowStorage
    {
        /// <summary>
        /// Get a flow run by its ID
        /// </summary>
        Task<FlowRun?> GetFlowRunAsync(Guid id);

        /// <summary>
        /// Get all flow runs for a user
        /// </summary>
        Task<IReadOnlyList<FlowRun>> GetFlowRunsForUserAsync(string userId, int limit = 100);

        /// <summary>
        /// Save a new flow run
        /// </summary>
        Task<Guid> SaveFlowRunAsync(FlowRun flowRun);

        /// <summary>
        /// Update a flow run's status
        /// </summary>
        Task UpdateFlowRunStatusAsync(Guid id, FlowRunStatus status);

        /// <summary>
        /// Update a flow run's completion time
        /// </summary>
        Task UpdateFlowRunCompletionTimeAsync(Guid id, DateTime completionTime);

        /// <summary>
        /// Update a flow run's error message
        /// </summary>
        Task UpdateFlowRunErrorMessageAsync(Guid id, string errorMessage);

        /// <summary>
        /// Save a flow element
        /// </summary>
        Task<Guid> SaveFlowElementAsync(FlowElement element);

        /// <summary>
        /// Get all elements for a flow run
        /// </summary>
        Task<IReadOnlyList<FlowElement>> GetFlowElementsAsync(Guid flowRunId);

        /// <summary>
        /// Save a flow result
        /// </summary>
        Task SaveFlowResultAsync(Guid flowRunId, object result);

        /// <summary>
        /// Get a flow result
        /// </summary>
        Task<object?> GetFlowResultAsync(Guid flowRunId);
    }
}
