using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly IAuthService _authService;

    public MeController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AuthMeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _authService.GetMeAsync(userId, cancellationToken);
        return Ok(response);
    }
}
