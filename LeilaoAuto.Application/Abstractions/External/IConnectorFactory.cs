namespace LeilaoAuto.Application.Abstractions.External;

public interface IConnectorFactory
{
    ILotConnector CreateByName(string name);
    IReadOnlyList<ILotConnector> CreateByDomain(string domain);
}
