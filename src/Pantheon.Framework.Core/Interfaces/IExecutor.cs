using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for flow executors
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// Submit a flow for execution
        /// </summary>
        /// <typeparam name="TInput">The input type for the flow</typeparam>
        /// <param name="flowName">The name of the flow to execute</param>
        /// <param name="input">The input for the flow</param>
        /// <param name="userId">The ID of the user submitting the flow</param>
        /// <returns>The ID of the flow run</returns>
        Task<Guid> SubmitFlowAsync<TInput>(string flowName, TInput input, string userId) where TInput : class;

        /// <summary>
        /// Get the status of a flow run
        /// </summary>
        /// <param name="flowRunId">The ID of the flow run</param>
        /// <returns>The status of the flow run</returns>
        Task<FlowRunStatus> GetFlowStatusAsync(Guid flowRunId);

        /// <summary>
        /// Get all elements produced by a flow run
        /// </summary>
        /// <param name="flowRunId">The ID of the flow run</param>
        /// <returns>The elements produced by the flow run</returns>
        Task<IReadOnlyList<FlowElement>> GetFlowElementsAsync(Guid flowRunId);

        /// <summary>
        /// Get the result of a flow run
        /// </summary>
        /// <param name="flowRunId">The ID of the flow run</param>
        /// <returns>The result of the flow run</returns>
        Task<object?> GetFlowResultAsync(Guid flowRunId);

        /// <summary>
        /// Cancel a flow run
        /// </summary>
        /// <param name="flowRunId">The ID of the flow run</param>
        /// <returns>True if the flow run was successfully canceled, false otherwise</returns>
        Task<bool> CancelFlowAsync(Guid flowRunId);
    }
}
