using LeilaoAuto.Application.Abstractions.External;
using LeilaoAuto.Application.Contracts.Connectors;
using LeilaoAuto.Application.Contracts.Lots;
using LeilaoAuto.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoAuto.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connectors")]
public class ConnectorsController : ControllerBase
{
    private readonly IConnectorRegistry _connectorRegistry;
    private readonly IConnectorFactory _connectorFactory;

    public ConnectorsController(
        IConnectorRegistry connectorRegistry,
        IConnectorFactory connectorFactory)
    {
        _connectorRegistry = connectorRegistry;
        _connectorFactory = connectorFactory;
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetConnectors()
    {
        var connectors = _connectorRegistry.GetAll()
            .Select(connector => new
            {
                connector.Name,
                connector.SupportedDomains
            })
            .ToList();

        return Ok(connectors);
    }

    [HttpPost("test/{name}")]
    [ProducesResponseType(typeof(ConnectorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestConnector(
        string name,
        CancellationToken cancellationToken,
        [FromBody] LotSearchFilterRequest? filters = null)
    {
        ILotConnector connector;
        try
        {
            connector = _connectorFactory.CreateByName(name);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Connector '{name}' not found." });
        }

        var rawItems = await connector.SearchAsync(filters ?? new LotSearchFilterRequest(), cancellationToken);
        var lots = new List<ProviderLotDto>(rawItems.Count);
        var notes = new List<string>();
        var discarded = 0;

        foreach (var raw in rawItems)
        {
            var parsed = await connector.ParseAsync(raw, cancellationToken);
            if (parsed is null)
            {
                discarded++;
                notes.Add("Item discarded during parse.");
                continue;
            }

            if (parsed.Status == LotStatus.Confirmed && !connector.ValidateLotUrl(parsed.LotUrl))
            {
                discarded++;
                notes.Add("Confirmed item discarded due to invalid lotUrl.");
                continue;
            }

            if (!connector.ValidateLotUrl(parsed.LotUrl))
            {
                discarded++;
                notes.Add("Item discarded due to invalid lotUrl.");
                continue;
            }

            lots.Add(parsed);
        }

        var result = new ConnectorResult(
            connector.Name,
            connector.SupportedDomains.ToArray(),
            rawItems.Count,
            lots.Count,
            discarded,
            lots,
            notes);

        return Ok(result);
    }
}
