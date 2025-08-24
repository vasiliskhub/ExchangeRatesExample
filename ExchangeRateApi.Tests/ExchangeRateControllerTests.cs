using ExchangeRateApi.Controllers;
using ExchangeRateApi.Models;
using ExchangeRateApi.Tests.TestHelpers;
using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace ExchangeRateApi.Tests;

[TestFixture]
public class ExchangeRateControllerTests
{
    private IExchangeRateProviderFactory _exchangeRateProviderFactory = null!;
    private ILogger<ExchangeRateController> _logger = null!;
    private ExchangeRateController _controller = null!;
    private IExchangeRateProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _exchangeRateProviderFactory = Substitute.For<IExchangeRateProviderFactory>();
        _logger = Substitute.For<ILogger<ExchangeRateController>>();
        _provider = Substitute.For<IExchangeRateProvider>();
        _controller = new ExchangeRateController(_exchangeRateProviderFactory, _logger);
    }

    #region GetExchangeRates (POST) Tests

    [Test]
    public async Task GetExchangeRates_ValidRequest_ReturnsOkWithRates()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "USD", "EUR" },
            TargetCurrency = "CZK"
        };

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            
            Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
            Assert.That(response.Rates, Has.Count.EqualTo(2));
            Assert.That(response.Rates[0].SourceCurrency, Is.EqualTo("USD"));
            Assert.That(response.Rates[0].TargetCurrency, Is.EqualTo("CZK"));
            Assert.That(response.Rates[0].Rate, Is.EqualTo(22.50m));
            Assert.That(response.Rates[1].SourceCurrency, Is.EqualTo("EUR"));
            Assert.That(response.Rates[1].TargetCurrency, Is.EqualTo("CZK"));
            Assert.That(response.Rates[1].Rate, Is.EqualTo(24.00m));

            // Verify logging
            _logger.VerifyLogInformation(1, "Received request for exchange rates with 2 currencies");
            _logger.VerifyLogInformation(1, "Successfully retrieved 2 exchange rates for base currency CZK");
        });

        // Verify provider factory was called correctly
        _exchangeRateProviderFactory.Received(1).GetProvider("CZK");
        await _provider.Received(1).GetExchangeRatesAsync(Arg.Is<IEnumerable<Currency>>(
            currencies => currencies.Count() == 2 && 
                         currencies.Any(c => c.Code == "USD") && 
                         currencies.Any(c => c.Code == "EUR")));
    }

    [Test]
    public async Task GetExchangeRates_DefaultBaseCurrency_UsesCZK()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "USD" },
            TargetCurrency = null // Should default to CZK
        };

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
			_exchangeRateProviderFactory.Received(1).GetProvider("CZK");
		});

        
    }

    [Test]
    public async Task GetExchangeRates_NullCurrencyCodes_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = null!,
            TargetCurrency = "CZK"
        };

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result.Result!;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            Assert.That(objectResult.Value, Is.EqualTo("An error occurred while retrieving exchange rates"));

            // Verify error logging using TestHelpers
            _logger.VerifyLogErrorContaining(1, "Unexpected error occurred while getting exchange rates");
        });

        // Verify provider factory was not called
        _exchangeRateProviderFactory.DidNotReceive().GetProvider(Arg.Any<string>());
    }

    [Test]
    public async Task GetExchangeRates_EmptyCurrencyCodes_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string>(),
            TargetCurrency = "CZK"
        };

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("At least one currency code must be provided"));

            // Verify logging
            _logger.VerifyLogWarning(1, "Exchange rate request received with empty currency codes");
        });
    }

    [Test]
    public async Task GetExchangeRates_WhitespaceCurrencyCodes_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { " ", "", "   " },
            TargetCurrency = "CZK"
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("No valid currency codes provided"));

            // Verify logging
            _logger.VerifyLogWarning(1, "No valid currency codes provided after filtering");
        });
    }

    [Test]
    public async Task GetExchangeRates_MixedValidAndInvalidCurrencies_FiltersAndProcessesValid()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "USD", "", "   ", "EUR", null! },
            TargetCurrency = "CZK"
        };

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            
            Assert.That(response.Rates, Has.Count.EqualTo(2));
		});

        await _provider.Received(1).GetExchangeRatesAsync(Arg.Is<IEnumerable<Currency>>(
            currencies => currencies.Count() == 2));
    }

    [Test]
    public async Task GetExchangeRates_CurrencyCodesConvertedToUpperCase()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "usd", "eur" },
            TargetCurrency = "CZK"
        };

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        await _provider.Received(1).GetExchangeRatesAsync(Arg.Is<IEnumerable<Currency>>(
            currencies => currencies.All(c => c.Code == c.Code.ToUpperInvariant())));
    }

    [Test]
    public async Task GetExchangeRates_InvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "USD" },
            TargetCurrency = "INVALID"
        };

        var exception = new InvalidOperationException("No provider for currency INVALID");
        _exchangeRateProviderFactory.When(x => x.GetProvider("INVALID")).Do(x => throw exception);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("No provider for currency INVALID"));

            // Verify error logging using TestHelpers
            _logger.VerifyLogErrorContaining(1, "Invalid operation when getting exchange rates", exception);
        });
    }

    [Test]
    public async Task GetExchangeRates_UnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ExchangeRateRequest
        {
            CurrencyCodes = new List<string> { "USD" },
            TargetCurrency = "CZK"
        };

        var exception = new Exception("Unexpected error");
        _exchangeRateProviderFactory.When(x => x.GetProvider("CZK")).Do(x => throw exception);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result.Result!;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            Assert.That(objectResult.Value, Is.EqualTo("An error occurred while retrieving exchange rates"));

            // Verify error logging using TestHelpers
            _logger.VerifyLogErrorContaining(1, "Unexpected error occurred while getting exchange rates", exception);
        });
    }

    #endregion

    #region GetExchangeRatesQuery (GET) Tests

    [Test]
    public async Task GetExchangeRatesQuery_ValidCurrencies_ReturnsOkWithRates()
    {
        // Arrange
        var currenciesParam = "USD,EUR,JPY";
        var baseCurrency = "CZK";

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m),
            new(new Currency("JPY"), new Currency("CZK"), 0.15m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRatesQuery(currenciesParam, baseCurrency);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            
            Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
            Assert.That(response.Rates, Has.Count.EqualTo(3));
        });

        await _provider.Received(1).GetExchangeRatesAsync(Arg.Is<IEnumerable<Currency>>(
            currencies => currencies.Count() == 3 && 
                         currencies.Any(c => c.Code == "USD") && 
                         currencies.Any(c => c.Code == "EUR") && 
                         currencies.Any(c => c.Code == "JPY")));
    }

    [Test]
    public async Task GetExchangeRatesQuery_CurrenciesWithSpaces_TrimsAndProcesses()
    {
        // Arrange
        var currenciesParam = " USD , EUR , JPY ";
        var baseCurrency = "CZK";

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m),
            new(new Currency("EUR"), new Currency("CZK"), 24.00m),
            new(new Currency("JPY"), new Currency("CZK"), 0.15m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRatesQuery(currenciesParam, baseCurrency);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            
            Assert.That(response.Rates, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task GetExchangeRatesQuery_DefaultBaseCurrency_UsesCZK()
    {
        // Arrange
        var currenciesParam = "USD";

        var exchangeRates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.50m)
        };

        _exchangeRateProviderFactory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(exchangeRates);

        // Act
        var result = await _controller.GetExchangeRatesQuery(currenciesParam, null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = (ExchangeRateResponse)okResult.Value!;
            
            Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
        });

        _exchangeRateProviderFactory.Received(1).GetProvider("CZK");
    }

    [Test]
    public async Task GetExchangeRatesQuery_EmptyCurrencies_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetExchangeRatesQuery("", null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("Currency codes parameter is required"));
        });

        _exchangeRateProviderFactory.DidNotReceive().GetProvider(Arg.Any<string>());
    }

    [Test]
    public async Task GetExchangeRatesQuery_WhitespaceCurrencies_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetExchangeRatesQuery("   ", null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("Currency codes parameter is required"));
        });
    }

    [Test]
    public async Task GetExchangeRatesQuery_NullCurrencies_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetExchangeRatesQuery(null!, null);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequestResult.Value, Is.EqualTo("Currency codes parameter is required"));
        });
    }

    #endregion

    #region GetAvailableProviders Tests

    [Test]
    public void GetAvailableProviders_ReturnsProvidersList()
    {
        // Act
        var result = _controller.GetAvailableProviders();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var response = okResult.Value;
            
            Assert.That(response, Is.Not.Null);
            
            // Verify the response contains providers information
            var providersProperty = response!.GetType().GetProperty("Providers");
            Assert.That(providersProperty, Is.Not.Null);
            
            var providers = (Array)providersProperty!.GetValue(response)!;
            Assert.That(providers, Has.Length.EqualTo(1));
        });
    }

    #endregion
}
