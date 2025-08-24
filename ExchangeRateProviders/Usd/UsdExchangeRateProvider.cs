using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Usd.Services;
using Microsoft.Extensions.Logging;

namespace ExchangeRateProviders.Usd;

public class UsdExchangeRateProvider : IExchangeRateProvider
{
    private readonly ILogger<UsdExchangeRateProvider> _logger;
	private readonly IUsdExchangeRateDataProvider _dataProvider;

	public UsdExchangeRateProvider(IUsdExchangeRateDataProvider dataProvider, ILogger<UsdExchangeRateProvider> logger)
	{
		_dataProvider = dataProvider;
		_logger = logger;
	}

	public string ExchangeRateProviderCurrencyCode => "USD";

    public async Task<IEnumerable<ExchangeRate>> GetExchangeRatesAsync(IEnumerable<Currency> currencies, CancellationToken cancellationToken = default)
    {
        if (currencies == null)
        {
            _logger.LogWarning("Requested currencies collection is null. Returning empty result.");
            return Enumerable.Empty<ExchangeRate>();
        }

        var requestedCurrencies = new HashSet<string>(currencies.Select(c => c.Code), StringComparer.OrdinalIgnoreCase);
        _logger.LogDebug("Fetching exchange rates for {Count} requested currencies via provider USD.", requestedCurrencies.Count);

        var allRates = await _dataProvider.GetDailyRatesAsync(cancellationToken);

		// Filter to only requested currencies
		var requestedRates = allRates.Where(r => requestedCurrencies.Contains(r.SourceCurrency.Code)).ToList();

        _logger.LogInformation("Provider USD returned {Filtered}/{Total} matching rates.", requestedRates.Count, allRates.Count());
        
        return requestedRates;
    }
}
