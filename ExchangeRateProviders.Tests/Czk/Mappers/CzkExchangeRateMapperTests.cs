using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Mappers;
using ExchangeRateProviders.Czk.Model;
using ExchangeRateProviders.Tests.TestHelpers;
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
        var expected = new List<(string Source, string Target, decimal Value)>
        {
            ("USD", Constants.ExchangeRateProviderCurrencyCode, 22.50m),
            ("EUR", Constants.ExchangeRateProviderCurrencyCode, 24.00m)
        };

        // Act
        var result = mapper.MapToExchangeRates(source).ToList();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++)
            {
                var exp = expected[i];
                Assert.That(result[i].SourceCurrency.Code, Is.EqualTo(exp.Source), $"Source at index {i}");
                Assert.That(result[i].TargetCurrency.Code, Is.EqualTo(exp.Target), $"Target at index {i}");
                Assert.That(result[i].Value, Is.EqualTo(exp.Value), $"Value at index {i}");
            }
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
            logger.VerifyLogDebug(1, "Skipping invalid rate entry for USD with non-positive amount 0");
            logger.VerifyLogDebug(1, "Skipping invalid rate entry for EUR with non-positive amount -5");
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
		Assert.That(result, Is.Empty);
	}
}
