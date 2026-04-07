using LeilaoAuto.Api.Extensions;
using LeilaoAuto.Application.Abstractions.Services;
using LeilaoAuto.Application.Contracts.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/monitoring")]
public class MonitoringController : ControllerBase
{
    private readonly IMonitoringService _monitoringService;

    public MonitoringController(IMonitoringService monitoringService)
    {
        _monitoringService = monitoringService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MonitoredVehicleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var response = await _monitoringService.GetByUserAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MonitoredVehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var created = await _monitoringService.AddAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MonitoredVehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMonitoredVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        var updated = await _monitoringService.UpdateAsync(userId, id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserIdOrThrow();
        await _monitoringService.RemoveAsync(userId, id, cancellationToken);
        return NoContent();
    }
}
