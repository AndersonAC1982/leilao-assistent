using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/opportunities")]
public class OpportunitiesController : ControllerBase
{
    private readonly IExperienceService _experienceService;

    public OpportunitiesController(IExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OpportunityFeedItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] OpportunityFeedQueryRequest query, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _experienceService.GetOpportunitiesAsync(userId, query, cancellationToken);
        return Ok(response);
    }
}
