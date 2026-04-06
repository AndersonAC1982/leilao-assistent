namespace LeilaoAuto.Application.Abstractions.External;

public interface IFipePriceProvider
{
    Task<decimal?> GetPriceByNormalizedModelAsync(string normalizedModel, int year, CancellationToken cancellationToken);
}
