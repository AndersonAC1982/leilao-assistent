using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Api.Authorization;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/lots")]
public class LotsController : ControllerBase
{
    private readonly ILotService _lotService;

    public LotsController(ILotService lotService)
    {
        _lotService = lotService;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(LotSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _lotService.SearchAsync(userId, filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<LotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Active([FromQuery] LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var response = await _lotService.GetActiveAsync(filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("closed")]
    [ProducesResponseType(typeof(IReadOnlyList<LotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Closed([FromQuery] LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var response = await _lotService.GetClosedAsync(filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _lotService.GetByIdAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("refresh")]
    [Authorize(Policy = PlanPolicies.PremiumOrHigher)]
    [ProducesResponseType(typeof(RefreshLotsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshedCount = await _lotService.RefreshAsync(cancellationToken);
        return Ok(new RefreshLotsResponse(refreshedCount));
    }
}
