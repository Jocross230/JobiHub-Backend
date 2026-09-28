using CVBuilder.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CVBuilder.API.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class AIJobSearchController : ControllerBase
{
    private readonly JobAggregationService _jobAggregationService;

    public AIJobSearchController(
        JobAggregationService jobAggregationService)
    {
        _jobAggregationService =
            jobAggregationService;
    }

    [AllowAnonymous]
    [HttpGet("ai-search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string? location)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new
            {
                message = "Job search query is required."
            });
        }

        try
        {
            var jobs =
                await _jobAggregationService.SearchJobsAsync(
                    query.Trim(),
                    location?.Trim() ?? "");

            return Ok(new
            {
                jobs
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "===== JOB AGGREGATION ERROR =====");

            Console.WriteLine(
                $"Message: {ex.Message}");

            Console.WriteLine(
                $"Details: {ex}");

            Console.WriteLine(
                "=================================");

            return StatusCode(
                500,
                new
                {
                    message =
                        "Job search failed.",
                    error = ex.Message
                });
        }
    }
}