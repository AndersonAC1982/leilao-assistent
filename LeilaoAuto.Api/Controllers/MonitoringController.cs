using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Monitoring;
using LeilaoAuto.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;

    public MonitoringController(IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [HttpGet("vehicles")]
    [ProducesResponseType(typeof(IReadOnlyList<MonitoredVehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVehicles(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _monitoringService.GetByUserAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("vehicles")]
    [ProducesResponseType(typeof(MonitoredVehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddVehicle([FromBody] CreateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();

        try
        {
            var created = await _monitoringService.AddAsync(userId, request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (DomainRuleException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("vehicles/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVehicle(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var removed = await _monitoringService.RemoveAsync(userId, id, cancellationToken);
        return removed ? NoContent() : NotFound();
    }
}
