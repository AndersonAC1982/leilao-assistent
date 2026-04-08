using LeilaoAuto.Application.Abstractions.External;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Registry central de conectores carregados via DI.
/// </summary>
public class ConnectorRegistry : IConnectorRegistry
{
    private readonly IReadOnlyList<ILotConnector> _connectors;

    public ConnectorRegistry(IEnumerable<ILotConnector> connectors)
    {
        _connectors = connectors
            .OrderBy(connector => connector.Name)
            .ToList();
    }

    public IReadOnlyList<ILotConnector> GetAll()
    {
        return _connectors;
    }

    public ILotConnector? GetByName(string name)
    {
        return _connectors.FirstOrDefault(connector =>
            connector.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ILotConnector> GetByDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return [];
        }

        var normalized = domain.Trim().ToLowerInvariant();
        return _connectors
            .Where(connector => connector.SupportedDomains.Any(supported =>
                supported.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                || normalized.Contains(supported, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
