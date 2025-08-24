using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;

namespace ExchangeRateProviders.Usd.Services
{
	public class UsdExchangeRateDataProviderService : IUsdExchangeRateDataProvider
	{
		public Task<IEnumerable<ExchangeRate>> GetDailyRatesAsync(CancellationToken cancellationToken)
		{
			//Return some hardcoded USD-based exchange rates for testing purposes
			var allRates = new List<ExchangeRate>
			{
				new(new Currency("EUR"), new Currency("USD"), 1.18m, DateTime.UtcNow), // EUR to USD
				new(new Currency("JPY"), new Currency("USD"), 0.009m, DateTime.UtcNow), // JPY to USD  
				new(new Currency("GBP"), new Currency("USD"), 1.33m, DateTime.UtcNow), // GBP to USD
				new(new Currency("AUD"), new Currency("USD"), 0.74m, DateTime.UtcNow), // AUD to USD
				new(new Currency("CAD"), new Currency("USD"), 0.80m, DateTime.UtcNow), // CAD to USD
				new(new Currency("CZK"), new Currency("USD"), 0.044m, DateTime.UtcNow), // CZK to USD
				new(new Currency("CHF"), new Currency("USD"), 1.10m, DateTime.UtcNow),   // CHF -> USD
				new(new Currency("SEK"), new Currency("USD"), 0.095m, DateTime.UtcNow),  // SEK -> USD
				new(new Currency("NOK"), new Currency("USD"), 0.093m, DateTime.UtcNow),  // NOK -> USD
				new(new Currency("DKK"), new Currency("USD"), 0.158m, DateTime.UtcNow),  // DKK -> USD
				new(new Currency("NZD"), new Currency("USD"), 0.61m, DateTime.UtcNow),   // NZD -> USD
				new(new Currency("CNY"), new Currency("USD"), 0.14m, DateTime.UtcNow),   // CNY -> USD
				new(new Currency("INR"), new Currency("USD"), 0.012m, DateTime.UtcNow),  // INR -> USD
				new(new Currency("BRL"), new Currency("USD"), 0.20m, DateTime.UtcNow),   // BRL -> USD
				new(new Currency("MXN"), new Currency("USD"), 0.058m, DateTime.UtcNow),  // MXN -> USD
				new(new Currency("ZAR"), new Currency("USD"), 0.052m, DateTime.UtcNow)   // ZAR -> USD
			};

			return Task.FromResult((IEnumerable<ExchangeRate>)allRates);
		}
	}
}
