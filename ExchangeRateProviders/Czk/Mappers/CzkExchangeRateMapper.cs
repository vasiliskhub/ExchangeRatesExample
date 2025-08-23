using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk.Model;
using Microsoft.Extensions.Logging;

namespace ExchangeRateProviders.Czk.Mappers;

public interface ICzkExchangeRatesMapper
{
    IEnumerable<ExchangeRate> MapToExchangeRates(IEnumerable<CnbApiExchangeRateDto> sourceRates);
}

public class CzkExchangeRateMapper : ICzkExchangeRatesMapper
{
    private readonly ILogger<CzkExchangeRateMapper> _logger;

    public CzkExchangeRateMapper(ILogger<CzkExchangeRateMapper> logger)
    {
        _logger = logger;
    }

    public IEnumerable<ExchangeRate> MapToExchangeRates(IEnumerable<CnbApiExchangeRateDto> sourceRates)
    {
        var targetCurrency = new Currency(Constants.ExchangeRateProviderCurrencyCode);
        foreach (var r in sourceRates)
        {
            if (r.Amount <= 0)
            {
                _logger.LogDebug("Skipping invalid rate entry for {Currency} with non-positive amount {Amount}", r.CurrencyCode, r.Amount);
                continue;
            }
            var sourceCurrency = new Currency(r.CurrencyCode.ToUpperInvariant());
            var perUnitRate = r.Rate / r.Amount;
            yield return new ExchangeRate(sourceCurrency, targetCurrency, perUnitRate);
        }
    }
}
