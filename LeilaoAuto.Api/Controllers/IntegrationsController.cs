using LeilaoAuto.Application.Abstractions.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly IFipePriceProvider _fipePriceProvider;

    public IntegrationsController(IFipePriceProvider fipePriceProvider)
    {
        _fipePriceProvider = fipePriceProvider;
    }

    [HttpGet("fipe/price")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFipePrice([FromQuery] string model, [FromQuery] int year, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model) || year < 1960 || year > DateTime.UtcNow.Year + 1)
        {
            return BadRequest(new { message = "Invalid model or year." });
        }

        var price = await _fipePriceProvider.GetPriceByNormalizedModelAsync(model.Trim().ToUpperInvariant(), year, cancellationToken);
        return Ok(new
        {
            model = model.Trim().ToUpperInvariant(),
            year,
            price
        });
    }
}
