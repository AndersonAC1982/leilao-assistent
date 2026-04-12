using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Experience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/scanner")]
public class ScannerController : ControllerBase
{
    private readonly IExperienceService _experienceService;

    public ScannerController(IExperienceService experienceService)
    {
        _experienceService = experienceService;
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(ScannerRunResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Run(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ScannerRunRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _experienceService.RunScannerAsync(userId, request, cancellationToken);
        return Ok(response);
    }
}
