using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/history")]
public class HistoryController : ControllerBase
{
    private readonly IExperienceService _experienceService;

    public HistoryController(IExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int take = 8, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _experienceService.GetHistoryAsync(userId, take, cancellationToken);
        return Ok(response);
    }
}
