using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pantheon.Framework.Core.Interfaces;
using Pantheon.Framework.Core.Models;

namespace Pantheon.Framework.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlowsController : ControllerBase
    {
        private readonly IExecutor _executor;
        private readonly IFlowStorage _flowStorage;
        private readonly ILogger<FlowsController> _logger;

        public FlowsController(
            IExecutor executor,
            IFlowStorage flowStorage,
            ILogger<FlowsController> logger)
        {
            _executor = executor;
            _flowStorage = flowStorage;
            _logger = logger;
        }

        [HttpPost("{flowName}")]
        public async Task<ActionResult<FlowRunResponse>> SubmitFlow(
            string flowName,
            [FromBody] JsonElement inputData,
            [FromHeader(Name = "X-User-Id")] string userId = "anonymous")
        {
            try
            {
                // Convert the input data to a string for logging
                string inputJson = inputData.GetRawText();
                _logger.LogInformation("Submitting flow {FlowName} with input {Input}", flowName, inputJson);

                // Submit the flow
                Guid flowRunId = await _executor.SubmitFlowAsync(flowName, inputData, userId);

                // Return the flow run ID
                var responseUrl = $"/api/flows/{flowRunId}";
                return Created(responseUrl, new FlowRunResponse
                {
                    Id = flowRunId,
                    Status = FlowRunStatus.Pending,
                    Links = new Dictionary<string, string>
                    {
                        { "self", responseUrl },
                        { "elements", $"{responseUrl}/elements" },
                        { "result", $"{responseUrl}/result" }
                    }
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid flow name: {FlowName}", flowName);
                return NotFound(new { message = $"Flow '{flowName}' not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting flow {FlowName}", flowName);
                return StatusCode(500, new { message = "Error submitting flow", error = ex.Message });
            }
        }

        [HttpGet("{flowRunId}")]
        public async Task<ActionResult<FlowRunResponse>> GetFlowStatus(Guid flowRunId)
        {
            try
            {
                // Get the flow run
                var flowRun = await _flowStorage.GetFlowRunAsync(flowRunId);
                if (flowRun == null)
                {
                    return NotFound(new { message = $"Flow run '{flowRunId}' not found" });
                }

                // Return the flow status
                var responseUrl = $"/api/flows/{flowRunId}";
                return Ok(new FlowRunResponse
                {
                    Id = flowRun.Id,
                    Status = flowRun.Status,
                    Links = new Dictionary<string, string>
                    {
                        { "self", responseUrl },
                        { "elements", $"{responseUrl}/elements" },
                        { "result", $"{responseUrl}/result" }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow status {FlowRunId}", flowRunId);
                return StatusCode(500, new { message = "Error getting flow status", error = ex.Message });
            }
        }

        [HttpGet("{flowRunId}/elements")]
        public async Task<ActionResult<IEnumerable<FlowElement>>> GetFlowElements(Guid flowRunId)
        {
            try
            {
                // Get the flow run
                var flowRun = await _flowStorage.GetFlowRunAsync(flowRunId);
                if (flowRun == null)
                {
                    return NotFound(new { message = $"Flow run '{flowRunId}' not found" });
                }

                // Get the flow elements
                var elements = await _flowStorage.GetFlowElementsAsync(flowRunId);
                return Ok(elements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow elements {FlowRunId}", flowRunId);
                return StatusCode(500, new { message = "Error getting flow elements", error = ex.Message });
            }
        }

        [HttpGet("{flowRunId}/result")]
        public async Task<ActionResult<object>> GetFlowResult(Guid flowRunId)
        {
            try
            {
                // Get the flow run
                var flowRun = await _flowStorage.GetFlowRunAsync(flowRunId);
                if (flowRun == null)
                {
                    return NotFound(new { message = $"Flow run '{flowRunId}' not found" });
                }

                // Check if the flow is completed
                if (flowRun.Status != FlowRunStatus.Completed)
                {
                    return BadRequest(new { message = $"Flow run '{flowRunId}' is not completed" });
                }

                // Get the flow result
                var result = await _flowStorage.GetFlowResultAsync(flowRunId);
                if (result == null)
                {
                    return NotFound(new { message = $"No result available for flow run '{flowRunId}'" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting flow result {FlowRunId}", flowRunId);
                return StatusCode(500, new { message = "Error getting flow result", error = ex.Message });
            }
        }

        [HttpDelete("{flowRunId}")]
        public async Task<ActionResult> CancelFlow(Guid flowRunId)
        {
            try
            {
                // Get the flow run
                var flowRun = await _flowStorage.GetFlowRunAsync(flowRunId);
                if (flowRun == null)
                {
                    return NotFound(new { message = $"Flow run '{flowRunId}' not found" });
                }

                // Cancel the flow
                bool canceled = await _executor.CancelFlowAsync(flowRunId);
                if (!canceled)
                {
                    return BadRequest(new { message = $"Could not cancel flow run '{flowRunId}'" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling flow {FlowRunId}", flowRunId);
                return StatusCode(500, new { message = "Error canceling flow", error = ex.Message });
            }
        }
    }

    public class FlowRunResponse
    {
        public Guid Id { get; set; }
        public FlowRunStatus Status { get; set; }
        public Dictionary<string, string> Links { get; set; } = new Dictionary<string, string>();
    }
}
