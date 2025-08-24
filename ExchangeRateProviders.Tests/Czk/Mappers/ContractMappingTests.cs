using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Mappers;
using ExchangeRateProviders.Czk.Model;
using NUnit.Framework;

namespace ExchangeRateProviders.Tests.Czk.Mappers;

[TestFixture]
public class ContractMappingTests
{
	[Test]
	public void MapToExchangeRate_AmountOne_ReturnsPerUnit()
	{
		// Arrange
		var dto = new CnbApiExchangeRateDto
		{
			CurrencyCode = "USD",
			Amount = 1,
			Rate = 22.50m,
			ValidFor = DateTime.UtcNow
		};

		// Act
		var result = dto.MapToExchangeRate();

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result.SourceCurrency.Code, Is.EqualTo("USD"));
			Assert.That(result.TargetCurrency.Code, Is.EqualTo(Constants.ExchangeRateProviderCurrencyCode));
			Assert.That(result.Value, Is.EqualTo(22.50m));
		});
	}

	[Test]
	public void MapToExchangeRate_MultiUnitAmount_Normalizes()
	{
		// Arrange
		var dto = new CnbApiExchangeRateDto
		{
			CurrencyCode = "JPY",
			Amount = 100,
			Rate = 17.00m,
			ValidFor = DateTime.UtcNow
		};

		// Act
		var result = dto.MapToExchangeRate();

		// Assert
		Assert.That(result.Value, Is.EqualTo(0.17m));
	}

	[Test]
	public void MapToExchangeRate_LowerCaseCurrency_UpperCases()
	{
		var dto = new CnbApiExchangeRateDto
		{
			CurrencyCode = "eur",
			Amount = 1,
			Rate = 24.00m,
			ValidFor = DateTime.UtcNow
		};

		var result = dto.MapToExchangeRate();

		Assert.That(result.SourceCurrency.Code, Is.EqualTo("EUR"));
	}

	[Test]
	public void MapToExchangeRate_Null_Throws()
	{
		CnbApiExchangeRateDto dto = null!;
		var ex = Assert.Throws<ArgumentNullException>(() => dto.MapToExchangeRate());
		Assert.That(ex!.ParamName, Is.EqualTo("dto"));
	}

	[TestCase(0)]
	[TestCase(-5)]
	public void MapToExchangeRate_NonPositiveAmount_Throws(int amount)
	{
		var dto = new CnbApiExchangeRateDto
		{
			CurrencyCode = "USD",
			Amount = amount,
			Rate = 10m,
			ValidFor = DateTime.UtcNow
		};

		var ex = Assert.Throws<ArgumentException>(() => dto.MapToExchangeRate());
		Assert.Multiple(() =>
		{
			Assert.That(ex!.ParamName, Is.EqualTo("dto"));
			Assert.That(ex.Message, Does.Contain("Amount must be positive"));
		});
	}

	[Test]
	public void MapToExchangeRates_ValidList_MapsAll()
	{
		// Arrange
		var now = DateTime.UtcNow;
		var source = new List<CnbApiExchangeRateDto>
		{
			new() { CurrencyCode = "USD", Amount = 1, Rate = 22.50m, ValidFor = now },
			new() { CurrencyCode = "EUR", Amount = 2, Rate = 48.00m, ValidFor = now }, // per-unit 24.00
            new() { CurrencyCode = "JPY", Amount = 100, Rate = 17.00m, ValidFor = now } // per-unit 0.17
        };

		// Act
		var result = source.MapToExchangeRates().ToList();

		// Assert
		Assert.Multiple(() =>
		{
			Assert.That(result, Has.Count.EqualTo(3));
			Assert.That(result[0].SourceCurrency.Code, Is.EqualTo("USD"));
			Assert.That(result[0].Value, Is.EqualTo(22.50m));
			Assert.That(result[1].SourceCurrency.Code, Is.EqualTo("EUR"));
			Assert.That(result[1].Value, Is.EqualTo(24.00m));
			Assert.That(result[2].SourceCurrency.Code, Is.EqualTo("JPY"));
			Assert.That(result[2].Value, Is.EqualTo(0.17m));
		});
	}

	[Test]
	public void MapToExchangeRates_NullSource_Throws()
	{
		IEnumerable<CnbApiExchangeRateDto> source = null!;
		var ex = Assert.Throws<ArgumentNullException>(() => source.MapToExchangeRates().ToList());
		Assert.That(ex!.ParamName, Is.EqualTo("sourceRates"));
	}

	[Test]
	public void MapToExchangeRates_InvalidEntry_ThrowsAndStops()
	{
		var source = new List<CnbApiExchangeRateDto>
		{
			new() { CurrencyCode = "USD", Amount = 1, Rate = 22.50m, ValidFor = DateTime.UtcNow },
			new() { CurrencyCode = "BAD", Amount = 0, Rate = 10m, ValidFor = DateTime.UtcNow }, // invalid
            new() { CurrencyCode = "EUR", Amount = 1, Rate = 24.00m, ValidFor = DateTime.UtcNow }
		};

		Assert.Throws<ArgumentException>(() => source.MapToExchangeRates().ToList());
	}
}