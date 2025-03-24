using System;
using System.Threading.Tasks;

namespace Pantheon.Framework.Core.Interfaces
{
    /// <summary>
    /// Interface for managing a queue of flows.
    /// 
    /// This interface defines the contract for a flow queue system which handles
    /// the lifecycle of flows through pending and in-progress states. It supports
    /// operations like pushing, popping, and requeuing flows as well as managing
    /// timeouts for in-progress flows.
    /// </summary>
    public interface IFlowQueue
    {
        /// <summary>
        /// Add a flow to the pending queue.
        /// </summary>
        /// <param name="flowId">The ID of the flow to be added to the pending queue.</param>
        Task PushToPendingAsync(Guid flowId);

        /// <summary>
        /// Remove and return the next flow from the pending queue, moving it to the in-progress queue.
        /// 
        /// This operation should be atomic to ensure the flow is not lost between queues.
        /// </summary>
        /// <returns>The ID of the next pending flow or null if the queue is empty.</returns>
        Task<Guid?> PopFromPendingAsync();

        /// <summary>
        /// Add a flow to the in-progress queue and set its timestamp.
        /// </summary>
        /// <param name="flowId">The ID of the flow to be added to the in-progress queue.</param>
        Task PushToInProgressAsync(Guid flowId);

        /// <summary>
        /// Remove a flow from the in-progress queue and its associated timestamp.
        /// </summary>
        /// <param name="flowId">The ID of the flow to be removed from the in-progress queue.</param>
        Task PopFromInProgressAsync(Guid flowId);

        /// <summary>
        /// Reset the timestamp for a flow in the in-progress queue.
        /// 
        /// This method is used to prevent a flow from being considered as expired
        /// by updating its timestamp.
        /// </summary>
        /// <param name="flowId">The ID of the flow whose timestamp should be reset.</param>
        Task ResetFlowTimeAsync(Guid flowId);

        /// <summary>
        /// Move expired flows from the in-progress queue back to the pending queue.
        /// 
        /// This method checks for flows that have exceeded their visibility timeout
        /// and moves them back to the pending queue for reprocessing.
        /// </summary>
        Task RequeueExpiredFlowsAsync();
    }
}
