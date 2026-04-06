using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Lots;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class LotsController : ControllerBase
{
    private readonly ILotService _lotService;

    public LotsController(ILotService lotService)
    {
        _lotService = lotService;
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<LotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchActive([FromQuery] LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _lotService.SearchActiveAsync(userId, filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<LotDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History([FromQuery] LotSearchFilterRequest filter, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _lotService.GetClosedHistoryBySimilarityAsync(userId, filter, cancellationToken);
        return Ok(response);
    }

    [HttpGet("exact")]
    [ProducesResponseType(typeof(LotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exact([FromQuery] ExactLotRequest request, CancellationToken cancellationToken)
    {
        var response = await _lotService.FindExactActiveAsync(request, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("averages")]
    [ProducesResponseType(typeof(IReadOnlyList<ModelAverageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Averages(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _lotService.GetModelAveragesByUserAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var syncedCount = await _lotService.SyncLatestLotsAsync(cancellationToken);
        return Ok(new { synced = syncedCount });
    }
}
