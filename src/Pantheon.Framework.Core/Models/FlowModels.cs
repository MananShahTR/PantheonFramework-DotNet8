using System;
using System.Collections.Generic;

namespace Pantheon.Framework.Core.Models
{
    /// <summary>
    /// Represents the status of a flow run
    /// </summary>
    public enum FlowRunStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Canceled
    }

    /// <summary>
    /// Represents a flow run instance
    /// </summary>
    public record FlowRun
    {
        public Guid Id { get; init; }
        public string FlowName { get; init; }
        public string UserId { get; init; }
        public FlowRunStatus Status { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime? CompletedAt { get; set; }
        public object? Input { get; init; }
        public object? Result { get; set; }
        public string? ErrorMessage { get; set; }

        public FlowRun(
            Guid id,
            string flowName,
            string userId,
            object? input = null)
        {
            Id = id;
            FlowName = flowName;
            UserId = userId;
            Status = FlowRunStatus.Pending;
            CreatedAt = DateTime.UtcNow;
            Input = input;
        }
    }

    /// <summary>
    /// Represents an element produced during a flow run
    /// </summary>
    public record FlowElement
    {
        public Guid Id { get; init; }
        public Guid FlowRunId { get; init; }
        public DateTime CreatedAt { get; init; }
        public object Content { get; init; }

        public FlowElement(
            Guid flowRunId,
            object content)
        {
            Id = Guid.NewGuid();
            FlowRunId = flowRunId;
            CreatedAt = DateTime.UtcNow;
            Content = content;
        }
    }

    /// <summary>
    /// Context for a flow run that can store the result
    /// </summary>
    /// <typeparam name="TResult">The type of result produced by the flow</typeparam>
    public class FlowRunContext<TResult> where TResult : class
    {
        private TResult? _result;

        public void SetResult(TResult result)
        {
            _result = result;
        }

        public TResult? Result => _result;
    }
}
