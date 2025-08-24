using ExchangeRateApi.Controllers;
using ExchangeRateApi.Models;
using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using FluentValidation;
using NSubstitute;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateApi.Tests;

[TestFixture]
public class ExchangeRateControllerTests
{
    private IExchangeRateProviderFactory _factory = null!;
    private ILogger<ExchangeRateController> _logger = null!;
    private IExchangeRateProvider _provider = null!;
    private IValidator<ExchangeRateRequest> _validator = null!;
    private ExchangeRateController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = Substitute.For<IExchangeRateProviderFactory>();
        _logger = Substitute.For<ILogger<ExchangeRateController>>();
        _provider = Substitute.For<IExchangeRateProvider>();
        _validator = Substitute.For<IValidator<ExchangeRateRequest>>();
        _controller = new ExchangeRateController(_factory, _logger, _validator);
    }

    [Test]
    public async Task GetExchangeRates_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>{"usd","eur"}, TargetCurrency = "CZK" };
        var rates = new List<ExchangeRate>
        {
            new(new Currency("USD"), new Currency("CZK"), 22.5m),
            new(new Currency("EUR"), new Currency("CZK"), 24.0m)
        };
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _factory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(rates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
            var response = (ExchangeRateResponse)((OkObjectResult)result.Result!).Value!;
            Assert.That(response.Rates, Has.Count.EqualTo(2));
            Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
        });
    }

    [Test]
    public async Task GetExchangeRates_ValidatorErrors_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>{"USD"}, TargetCurrency = "CZK" };
        var failures = new List<FluentValidation.Results.ValidationFailure>{ new("CurrencyCodes","Invalid") };
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult(failures));

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var bad = (BadRequestObjectResult)result.Result!;
            Assert.That(bad.Value, Is.InstanceOf<string[]>());
        });
    }

    [Test]
    public async Task GetExchangeRates_NoCodes_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>(), TargetCurrency = "CZK" };

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetExchangeRates_DefaultTargetCurrency_WhenNull()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>{"USD"}, TargetCurrency = null };
        var rates = new List<ExchangeRate>{ new(new Currency("USD"), new Currency("CZK"), 22.5m)};
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _factory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(rates);

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        var response = (ExchangeRateResponse)((OkObjectResult)result.Result!).Value!;
        Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
    }

    [Test]
    public async Task GetExchangeRates_InvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>{"USD"}, TargetCurrency = "XXX" };
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _factory.When(f => f.GetProvider("XXX")).Do(_ => throw new InvalidOperationException("no provider"));

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetExchangeRates_Exception_Returns500()
    {
        // Arrange
        var request = new ExchangeRateRequest { CurrencyCodes = new List<string>{"USD"}, TargetCurrency = "CZK" };
        _validator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());
        _factory.GetProvider("CZK").Returns(_provider);
        _provider.When(p => p.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()))
            .Do(_ => throw new Exception("boom"));

        // Act
        var result = await _controller.GetExchangeRates(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<ObjectResult>());
            var obj = (ObjectResult)result.Result!;
            Assert.That(obj.StatusCode, Is.EqualTo(500));
        });
    }

    // GET endpoint tests
    [Test]
    public async Task GetExchangeRatesQuery_Valid_ReturnsOk()
    {
        // Arrange
        var rates = new List<ExchangeRate>{ new(new Currency("USD"), new Currency("CZK"), 22.5m)};
        _factory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(rates);
        _validator.ValidateAsync(Arg.Any<ExchangeRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.GetExchangeRatesQuery("USD", "CZK");

        // Assert
        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
    }

    [Test]
    public async Task GetExchangeRatesQuery_InvalidFormat_ReturnsBadRequest()
    {
        // Arrange / Act
        var result = await _controller.GetExchangeRatesQuery("USDX,EUR", "CZK");

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetExchangeRatesQuery_TooManyCodes_ReturnsBadRequest()
    {
        // Arrange (codes variable kept for clarity even if not used directly below)
        var codes = string.Join(',', Enumerable.Range(0,11).Select(i => $"A{i:00}".Substring(0,3))); // will violate regex too but length first

        // Act
        var result = await _controller.GetExchangeRatesQuery("USD,EUR,JPY,AAA,BBB,CCC,DDD,EEE,FFF,GGG,HHH", "CZK");

        // Assert
        Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetExchangeRatesQuery_DefaultTarget_WhenNull()
    {
        // Arrange
        var rates = new List<ExchangeRate>{ new(new Currency("USD"), new Currency("CZK"), 22.5m)};
        _factory.GetProvider("CZK").Returns(_provider);
        _provider.GetExchangeRatesAsync(Arg.Any<IEnumerable<Currency>>()).Returns(rates);
        _validator.ValidateAsync(Arg.Any<ExchangeRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new FluentValidation.Results.ValidationResult());

        // Act
        var result = await _controller.GetExchangeRatesQuery("USD", null);

        // Assert
        var ok = (OkObjectResult)result.Result!;
        var response = (ExchangeRateResponse)ok.Value!;
        Assert.That(response.TargetCurrency, Is.EqualTo("CZK"));
    }
}
