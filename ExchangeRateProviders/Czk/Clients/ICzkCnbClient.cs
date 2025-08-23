using ExchangeRateProviders.Czk.Model;

namespace ExchangeRateProviders.Czk.Clients;

public interface ICzkCnbClient
{
    Task<IReadOnlyList<CnbApiExchangeRateDto>> GetDailyRatesRawAsync(CancellationToken cancellationToken = default);
}
