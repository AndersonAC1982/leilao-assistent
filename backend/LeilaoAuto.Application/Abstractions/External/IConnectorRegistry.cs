namespace LeilaoAuto.Application.Abstractions.External;

public interface IConnectorRegistry
{
    IReadOnlyList<ILotConnector> GetAll();
    ILotConnector? GetByName(string name);
    IReadOnlyList<ILotConnector> GetByDomain(string domain);
}
