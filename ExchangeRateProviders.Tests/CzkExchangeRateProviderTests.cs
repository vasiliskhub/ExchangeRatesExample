using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ExchangeRateProviders.Tests;

[TestFixture]
public class CzkExchangeRateProviderTests
{
    [Test]
    public async Task GetExchangeRatesAsync_NullCurrencies_ReturnsEmpty()
    {
        var dataProvider = Substitute.For<IExchangeRateDataProvider>();
        var logger = Substitute.For<ILogger<CzkExchangeRateProvider>>();
        var provider = new CzkExchangeRateProvider(dataProvider, logger);

        var result = await provider.GetExchangeRatesAsync(null!);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetExchangeRatesAsync_FiltersToRequestedCurrencies()
    {
        var logger = Substitute.For<ILogger<CzkExchangeRateProvider>>();
        var dataProvider = Substitute.For<IExchangeRateDataProvider>();
        var allRates = new[]
        {
            new ExchangeRate(new Currency("USD"), new Currency("CZK"), 22.5m),
            new ExchangeRate(new Currency("EUR"), new Currency("CZK"), 24.0m),
            new ExchangeRate(new Currency("JPY"), new Currency("CZK"), 0.17m)
        };
        dataProvider.GetDailyRatesAsync().Returns(allRates);
        var provider = new CzkExchangeRateProvider(dataProvider, logger);

        var requested = new[] { new Currency("USD"), new Currency("JPY") };
        var result = (await provider.GetExchangeRatesAsync(requested)).ToList();

        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.Any(r => r.SourceCurrency.Code == "USD"));
        Assert.That(result.Any(r => r.SourceCurrency.Code == "JPY"));
        Assert.That(result.All(r => r.TargetCurrency.Code == "CZK"));
    }
}
