# Queued Executor Usage Example

This document demonstrates how to use the QueuedExecutor with InMemoryFlowQueue to run multiple flows concurrently with throttling.

## Basic Setup

First, set up the necessary components:

```csharp
// Create the flow storage
var flowStorage = new InMemoryFlowStorage();

// Create the flow queue with a visibility timeout of 10 seconds
var flowQueue = new InMemoryFlowQueue(visibilityTimeoutSeconds: 10);

// Register your flows
var flows = new Dictionary<string, IFlow<object, object, object>>
{
    { "myFlow", CreateGenericFlow(specificFlow) }
};

// Create the queued executor with a maximum of 2 concurrent flows
var executor = new QueuedExecutor(
    flowStorage,
    flowQueue,
    flows,
    maxConcurrentFlows: 2,
    logger);
```

## Submitting Multiple Flows

The QueuedExecutor will automatically manage flow execution, running up to the specified maximum number of concurrent flows and queuing the rest:

```csharp
// Submit multiple flows
var flowIds = new List<Guid>();
for (int i = 0; i < 5; i++)
{
    var input = new MyInput { ... };
    var flowRunId = await executor.SubmitFlowAsync("myFlow", input, "user123");
    flowIds.Add(flowRunId);
    Console.WriteLine($"Submitted flow #{i+1} with ID: {flowRunId}");
}
```

## Waiting for Completion

You can wait for all flows to complete:

```csharp
// Wait for all flows to complete
bool allCompleted;
do
{
    await Task.Delay(500);
    allCompleted = true;
    
    foreach (var flowId in flowIds)
    {
        var status = await executor.GetFlowStatusAsync(flowId);
        if (status != FlowRunStatus.Completed && 
            status != FlowRunStatus.Failed && 
            status != FlowRunStatus.Canceled)
        {
            allCompleted = false;
            break;
        }
    }
} while (!allCompleted);

Console.WriteLine("All flows completed!");
```

## Queue Management

The flow queue system automatically manages flow states:

1. **Pending Queue**: New flows are added to the pending queue.
2. **In-Progress Queue**: When execution capacity is available, flows move from pending to in-progress.
3. **Visibility Timeout**: If a flow takes longer than the visibility timeout, it's automatically moved back to the pending queue for retry.
4. **Completed/Failed Flows**: When a flow completes or fails, it's removed from the in-progress queue.

## Proper Shutdown

Always shut down the QueuedExecutor properly when your application is ending:

```csharp
// Stop the queue processor
await executor.StopAsync();
```

## Key Benefits

1. **Controlled Concurrency**: Limit the number of flows executing simultaneously to prevent resource exhaustion.
2. **Automatic Retries**: Flows that time out are automatically requeued.
3. **Fair Scheduling**: Flows are processed in the order they were submitted.
4. **Resilience**: Failed flows don't affect the execution of other flows.

## Implementation Details

The InMemoryFlowQueue uses thread-safe operations with a SemaphoreSlim to prevent race conditions when multiple threads access the queue simultaneously. The QueuedExecutor runs a background task that continuously checks for new pending flows and executes them when capacity is available.
