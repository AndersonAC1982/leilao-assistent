using LeilaoAuto.Application.Abstractions.External;

namespace LeilaoAuto.Infrastructure.External.Connectors;

/// <summary>
/// Factory de conectores por nome e por dominio.
/// </summary>
public class ConnectorFactory : IConnectorFactory
{
    private readonly IConnectorRegistry _registry;

    public ConnectorFactory(IConnectorRegistry registry)
    {
        _registry = registry;
    }

    public ILotConnector CreateByName(string name)
    {
        var connector = _registry.GetByName(name);
        if (connector is not null)
        {
            return connector;
        }

        var available = string.Join(", ", _registry.GetAll().Select(item => item.Name));
        throw new KeyNotFoundException($"Connector '{name}' not found. Available connectors: {available}");
    }

    public IReadOnlyList<ILotConnector> CreateByDomain(string domain)
    {
        return _registry.GetByDomain(domain);
    }
}
