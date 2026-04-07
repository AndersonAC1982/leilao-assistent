using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("average-price")]
    [ProducesResponseType(typeof(IReadOnlyList<ModelAveragePriceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAveragePrice([FromQuery] string? model, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _analyticsService.GetAveragePriceAsync(userId, model, cancellationToken);
        return Ok(response);
    }

    [HttpGet("opportunities")]
    [ProducesResponseType(typeof(IReadOnlyList<OpportunityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? model, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _analyticsService.GetOpportunitiesAsync(userId, model, cancellationToken);
        return Ok(response);
    }

    [HttpGet("risk-summary")]
    [ProducesResponseType(typeof(RiskSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRiskSummary([FromQuery] string? model, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _analyticsService.GetRiskSummaryAsync(userId, model, cancellationToken);
        return Ok(response);
    }
}
