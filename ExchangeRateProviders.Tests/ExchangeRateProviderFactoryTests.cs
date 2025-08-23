using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Czk;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ExchangeRateProviders.Tests;

[TestFixture]
public class ExchangeRateProviderFactoryTests
{
    [Test]
    public void GetProvider_UnknownCurrency_Throws()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExchangeRateProviderFactory>>();
        var factory = new ExchangeRateProviderFactory(Array.Empty<IExchangeRateProvider>(), logger);

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => factory.GetProvider("USD"));
	}

    [Test]
    public void GetProvider_KnownCurrency_ReturnsProvider()
    {
        // Arrange
        var providerLogger = Substitute.For<ILogger<CzkExchangeRateProvider>>();
        var dataProvider = Substitute.For<IExchangeRateDataProvider>();
        var provider = new CzkExchangeRateProvider(dataProvider, providerLogger);
        var factoryLogger = Substitute.For<ILogger<ExchangeRateProviderFactory>>();
        var factory = new ExchangeRateProviderFactory(new[] { provider }, factoryLogger);

        // Act
        var resolved = factory.GetProvider(Constants.ExchangeRateProviderCurrencyCode);

		// Assert
		Assert.That(resolved, Is.SameAs(provider));
	}
}
