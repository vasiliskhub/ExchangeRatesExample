using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk.Clients;
using ExchangeRateProviders.Czk.Mappers;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace ExchangeRateProviders.Czk.Services
{
	public class CzkExchangeRateDataProviderSevice : IExchangeRateDataProvider
	{
		private const string CacheKey = "CnbDailyRates";
		private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

		private readonly IFusionCache _cache;
		private readonly ICzkCnbClient _apiClient;
		private readonly ILogger<CzkExchangeRateDataProviderSevice> _logger;

		public CzkExchangeRateDataProviderSevice(
			IFusionCache cache,
			ICzkCnbClient apiClient,
			ILogger<CzkExchangeRateDataProviderSevice> logger)
		{
			_cache = cache;
			_apiClient = apiClient;
			_logger = logger;
		}

		public async Task<IEnumerable<ExchangeRate>> GetDailyRatesAsync()
		{
			return await _cache.GetOrSetAsync(CacheKey, async _ =>
			{
				_logger.LogInformation("Cache miss for CNB daily rates. Fetching and mapping.");
				var raw = await _apiClient.GetDailyRatesRawAsync().ConfigureAwait(false);
				var mapped = raw.MapToExchangeRates();
				_logger.LogInformation("Mapped {Count} CNB exchange rates (base {BaseCurrency}).", mapped.Count(), Constants.ExchangeRateProviderCurrencyCode);
				return (IEnumerable<ExchangeRate>)mapped;
			}, new FusionCacheEntryOptions { Duration = CacheDuration });
		}
	}
}
