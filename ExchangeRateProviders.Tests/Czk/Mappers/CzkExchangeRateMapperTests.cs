using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Mappers;
using ExchangeRateProviders.Czk.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ExchangeRateProviders.Tests.Czk.Mappers;

[TestFixture]
public class CzkExchangeRateMapperTests
{
    [Test]
    public void MapToExchangeRates_ValidEntries_MapsAllWithPerUnit()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CzkExchangeRateMapper>>();
        var mapper = new CzkExchangeRateMapper(logger);
        var now = DateTime.UtcNow;
        var source = new List<CnbApiExchangeRateDto>
        {
            new() { CurrencyCode = "USD", Amount = 1, Rate = 22.50m, ValidFor = now },
            new() { CurrencyCode = "EUR", Amount = 2, Rate = 48.00m, ValidFor = now }
        };

        // Act
        var result = mapper.MapToExchangeRates(source).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].SourceCurrency.Code, Is.EqualTo("USD"));
            Assert.That(result[0].TargetCurrency.Code, Is.EqualTo(Constants.ExchangeRateProviderCurrencyCode));
            Assert.That(result[0].Value, Is.EqualTo(22.50m));
            Assert.That(result[1].SourceCurrency.Code, Is.EqualTo("EUR"));
			Assert.That(result[1].TargetCurrency.Code, Is.EqualTo(Constants.ExchangeRateProviderCurrencyCode));
			Assert.That(result[1].Value, Is.EqualTo(24.00m));
        });
    }

    [Test]
    public void MapToExchangeRates_LowerCaseCurrency_UpperCases()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CzkExchangeRateMapper>>();
        var mapper = new CzkExchangeRateMapper(logger);
        var source = new[] { new CnbApiExchangeRateDto { CurrencyCode = "gbp", Amount = 1, Rate = 28.10m, ValidFor = DateTime.UtcNow } };

        // Act
        var result = mapper.MapToExchangeRates(source).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].SourceCurrency.Code, Is.EqualTo("GBP"));
        });
    }

    [Test]
    public void MapToExchangeRates_InvalidAmount_Skips()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CzkExchangeRateMapper>>();
        var mapper = new CzkExchangeRateMapper(logger);
        var source = new[]
        {
            new CnbApiExchangeRateDto { CurrencyCode = "USD", Amount = 0, Rate = 22.50m, ValidFor = DateTime.UtcNow }, 
            new CnbApiExchangeRateDto { CurrencyCode = "EUR", Amount = -5, Rate = 120m, ValidFor = DateTime.UtcNow }, 
            new CnbApiExchangeRateDto { CurrencyCode = "JPY", Amount = 100, Rate = 17.00m, ValidFor = DateTime.UtcNow }
        };

        // Act
        var result = mapper.MapToExchangeRates(source).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].SourceCurrency.Code, Is.EqualTo("JPY"));
            Assert.That(result[0].Value, Is.EqualTo(0.17m));
        });
    }

    [Test]
    public void MapToExchangeRates_EmptyInput_ReturnsEmpty()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CzkExchangeRateMapper>>();
        var mapper = new CzkExchangeRateMapper(logger);

        // Act
        var result = mapper.MapToExchangeRates(Array.Empty<CnbApiExchangeRateDto>()).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
        });
    }
}
