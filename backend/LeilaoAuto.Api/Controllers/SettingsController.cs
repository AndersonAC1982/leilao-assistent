using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly IExperienceService _experienceService;

    public SettingsController(IExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _experienceService.GetSettingsAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPut]
    [ProducesResponseType(typeof(UserSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Put([FromBody] UpdateUserSettingsRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _experienceService.UpdateSettingsAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
