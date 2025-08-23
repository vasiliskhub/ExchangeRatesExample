using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk.Services;
using ExchangeRateProviders.Czk.Clients;
using ExchangeRateProviders.Czk.Mappers;
using ExchangeRateProviders.Czk.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using ZiggyCreatures.Caching.Fusion;

namespace ExchangeRateProviders.Tests.Czk.Services;

[TestFixture]
public class CzkExchangeRateDataProviderSeviceTests
{
    [Test]
    public async Task GetDailyRatesAsync_FirstCall_FetchesAndMaps()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        var apiClient = Substitute.For<ICzkCnbClient>();
        var mapper = Substitute.For<ICzkExchangeRatesMapper>();
        var logger = Substitute.For<ILogger<CzkExchangeRateDataProviderSevice>>();
        var service = new CzkExchangeRateDataProviderSevice(cache, apiClient, mapper, logger);

        var raw = new List<CnbApiExchangeRateDto>
        {
            new() { CurrencyCode = "USD", Amount = 1, Rate = 22.50m, ValidFor = DateTime.UtcNow },
            new() { CurrencyCode = "EUR", Amount = 1, Rate = 24.00m, ValidFor = DateTime.UtcNow }
        };
        var mapped = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m)
        };

        apiClient.GetDailyRatesRawAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<CnbApiExchangeRateDto>>(raw));
        mapper.MapToExchangeRates(Arg.Is<IEnumerable<CnbApiExchangeRateDto>>(r => ReferenceEquals(r, raw))).Returns(mapped);

        // Act
        var result = (await service.GetDailyRatesAsync()).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].SourceCurrency.Code, Is.EqualTo("USD"));
            Assert.That(result[1].SourceCurrency.Code, Is.EqualTo("EUR"));
            apiClient.Received(1).GetDailyRatesRawAsync(Arg.Any<CancellationToken>());
            mapper.Received(1).MapToExchangeRates(Arg.Any<IEnumerable<CnbApiExchangeRateDto>>());
        });
    }

    [Test]
    public async Task GetDailyRatesAsync_SubsequentCall_UsesCache()
    {
        // Arrange
        var cache = new FusionCache(new FusionCacheOptions());
        var apiClient = Substitute.For<ICzkCnbClient>();
        var mapper = Substitute.For<ICzkExchangeRatesMapper>();
        var logger = Substitute.For<ILogger<CzkExchangeRateDataProviderSevice>>();
        var service = new CzkExchangeRateDataProviderSevice(cache, apiClient, mapper, logger);

        var raw = new List<CnbApiExchangeRateDto>
        {
            new() { CurrencyCode = "JPY", Amount = 100, Rate = 17.00m, ValidFor = DateTime.UtcNow }
        };
        var mapped = new List<ExchangeRate>
        {
            new(new Currency("JPY"), new Currency("CZK"), 0.17m)
        };

        apiClient.GetDailyRatesRawAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<CnbApiExchangeRateDto>>(raw));
        mapper.MapToExchangeRates(Arg.Any<IEnumerable<CnbApiExchangeRateDto>>()).Returns(mapped);

        // Act
        var first = (await service.GetDailyRatesAsync()).ToList();
        var second = (await service.GetDailyRatesAsync()).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second)); // same cached content
            apiClient.Received(1).GetDailyRatesRawAsync(Arg.Any<CancellationToken>()); // factory executed only once
            mapper.Received(1).MapToExchangeRates(Arg.Any<IEnumerable<CnbApiExchangeRateDto>>());
        });
    }
}
