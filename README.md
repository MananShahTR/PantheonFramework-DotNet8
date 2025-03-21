# Pantheon Framework (.NET 8 Implementation)

A .NET 8 implementation of the [Pantheon Framework](https://github.com/casetext/pantheon-framework), a toolkit for building AI skills with large language models (LLMs).

## Overview

The Pantheon Framework provides a structured way to build applications that use LLMs. It's designed to be modular, extensible, and easy to use. The framework includes:

- **Templates**: Define prompts and parse results
- **Flows**: Orchestrate LLM calls and other operations
- **Storage**: Store flow runs, elements, and results
- **Executors**: Run flows and manage their lifecycle
- **API**: HTTP endpoints for interacting with flows
- **CLI**: Command-line interface for running flows

## Project Structure

The framework is organized into the following projects:

- **Pantheon.Framework.Core**: Core interfaces and models
- **Pantheon.Framework.Template**: Template engine using Scriban
- **Pantheon.Framework.LlmClient**: Client for interacting with LLMs
- **Pantheon.Framework.Flow**: Flow implementation
- **Pantheon.Framework.Storage**: Storage implementations (in-memory)
- **Pantheon.Framework.Executors**: Executor implementations (in-memory)
- **Pantheon.Framework.Api**: API endpoints for flows
- **Pantheon.Framework.Cli**: Command-line interface for flows

## Getting Started

### Prerequisites

- .NET 8 SDK

### Building the Framework

```bash
cd PantheonFramework
dotnet build
```

### Running the Samples

#### SimpleSkill

The SimpleSkill sample demonstrates how to build a simple flow that summarizes text:

```bash
cd PantheonFramework/samples/SimpleSkill
dotnet run
```

The sample will:
1. Create a mock LLM client
2. Define a template for summarizing text
3. Create a flow that uses the template
4. Execute the flow with some input text
5. Display the results

## Core Concepts

### Templates

Templates are rendered with input data to create prompts for LLMs. They can then parse the LLM response into a structured output format.

Example:

```csharp
// Create a template for summarization
var templateContent = "Please summarize the following text:\n\n{{ Text }}\n\nProvide a concise summary:";
var outputParser = new OutputParser<SummarizeResult>(response => new SummarizeResult { Summary = response });
var template = new Template<SummarizeInput, SummarizeResult>("summarize", templateContent, outputParser);

// Use a template runner to execute the template
var result = await templateRunner.ExecuteAsync(template, input, 100);
```

### Flows

Flows orchestrate LLM calls and other operations, producing a stream of elements and a final result.

Example:

```csharp
// Define a flow function
async IAsyncEnumerable<SummarizeElement> SummarizeFlowFunc(FlowRunContext<SummarizeResult> context, SummarizeInput input)
{
    // Yield elements during flow execution
    yield return new SummarizeElement { Step = "Starting summarization" };
    
    // Execute template and set result
    var result = await templateRunner.ExecuteAsync(summarizeTemplate, input, 100);
    context.SetResult(result);
    
    yield return new SummarizeElement { Step = "Summarization complete" };
}

// Create a flow
var flow = new Flow<SummarizeInput, SummarizeElement, SummarizeResult>(
    "summarize",
    SummarizeFlowFunc,
    typeof(SummarizeInput),
    typeof(SummarizeElement),
    typeof(SummarizeResult));
```

### Executors

Executors run flows and manage their lifecycle, providing methods to submit flows, check their status, get elements, get results, and cancel flows.

Example:

```csharp
// Create an executor
var flows = new Dictionary<string, IFlow<object, object, object>> { { "summarize", summarizeFlow } };
var executor = new InMemoryExecutor(flowStorage, flows);

// Submit a flow
var flowRunId = await executor.SubmitFlowAsync("summarize", input, "user123");

// Get the flow status
var status = await executor.GetFlowStatusAsync(flowRunId);

// Get the flow elements
var elements = await executor.GetFlowElementsAsync(flowRunId);

// Get the flow result
var result = await executor.GetFlowResultAsync(flowRunId);
```

## API

The Pantheon Framework includes a RESTful API for interacting with flows:

- `POST /api/flows/{flowName}`: Submit a flow
- `GET /api/flows/{flowRunId}`: Get the status of a flow run
- `GET /api/flows/{flowRunId}/elements`: Get the elements produced by a flow run
- `GET /api/flows/{flowRunId}/result`: Get the result of a flow run
- `DELETE /api/flows/{flowRunId}`: Cancel a flow run

## CLI

The Pantheon Framework includes a command-line interface for interacting with flows:

- `pantheon run {flowName} --input {inputFile} --user {userId}`: Run a flow
- `pantheon status {flowRunId}`: Get the status of a flow run
- `pantheon elements {flowRunId}`: Get the elements produced by a flow run
- `pantheon result {flowRunId}`: Get the result of a flow run
- `pantheon cancel {flowRunId}`: Cancel a flow run

## Building Your Own Flows

To build your own flows:

1. Define input, element, and result models
2. Create templates with Scriban syntax
3. Implement flow functions
4. Register flows with an executor
5. Use the API, CLI, or call the executor directly

## License

This project is licensed under the MIT License - see the LICENSE file for details.
