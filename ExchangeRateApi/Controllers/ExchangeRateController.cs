using ExchangeRateApi.Models;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.RegularExpressions;

namespace ExchangeRateApi.Controllers;

[ApiController]
[SwaggerTag("Exchange Rate operations for retrieving currency exchange rates")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateController> _logger;
    private readonly IValidator<ExchangeRateRequest>? _requestValidator;
    private static readonly Regex QueryCodesRegex = new("^[A-Za-z]{3}(?:,[A-Za-z]{3})*$", RegexOptions.Compiled);
    private const string DefaultTargetCurrency = "CZK";

	public ExchangeRateController(
		IExchangeRateService exchangeRateService, 
        ILogger<ExchangeRateController> logger,
        IValidator<ExchangeRateRequest>? requestValidator = null)
    {
		_exchangeRateService = exchangeRateService;
        _logger = logger;
        _requestValidator = requestValidator;
    }

    /// <summary>
    /// Get exchange rates for specified currencies using JSON request
    /// </summary>
    /// <param name="request">The exchange rate request containing currency codes and optional target currency</param>
    /// <param name="cancellationToken">Request cancellation token</param>
    /// <returns>Exchange rates for the requested currencies</returns>
    /// <response code="200">Returns the exchange rates for the requested currencies</response>
    /// <response code="400">If the request is invalid or no currency codes are provided</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost(ApiEndpoints.ExchangeRates.GetAllByRequestBody)]
    [SwaggerOperation(
        Summary = "Get exchange rates using POST request",
        Description = "Retrieves exchange rates for specified currencies using a JSON request body. Supports multiple currencies and custom target currency.",
        OperationId = "GetExchangeRatesPost")]
    [SwaggerResponse(200, "Exchange rates retrieved successfully", typeof(ExchangeRateResponse))
    ]
    [SwaggerResponse(400, "Invalid request - missing or invalid currency codes")]
    [SwaggerResponse(500, "Internal server error")]
	public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRates(
	[FromBody] ExchangeRateRequest request,
	CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Received request for exchange rates with {Count} currencies", request.CurrencyCodes.Count);

		if (request.CurrencyCodes == null || !request.CurrencyCodes.Any())
		{
			_logger.LogWarning("Exchange rate request received with empty currency codes");
			throw new ArgumentException("At least one currency code must be provided");
		}

		if (_requestValidator != null)
		{
			ValidationResult validationResult = await _requestValidator.ValidateAsync(request, cancellationToken);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
				_logger.LogWarning("Validation failed for request: {Errors}", string.Join(", ", errors));
				throw new ArgumentException(string.Join("; ", errors));
			}
		}

		var currencies = request.CurrencyCodes
		.Where(code => !string.IsNullOrWhiteSpace(code))
		.Select(code => new Currency(code.ToUpperInvariant()))
		.ToList();

        var targetCurrency = request.TargetCurrency?.ToUpperInvariant() ?? DefaultTargetCurrency;

		var currencyRates = await GetExchangeRatesForCurrenciesAsync(targetCurrency, currencies, cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} exchange rates for target currency {TargetCurrency}",
			currencyRates.Count(), targetCurrency);

		var response = new ExchangeRateResponse
		{
			TargetCurrency = targetCurrency,
			Rates = currencyRates.Select(rate => new ExchangeRateDto
			{
				SourceCurrency = rate.SourceCurrency.Code,
				TargetCurrency = rate.TargetCurrency.Code,
				Rate = rate.Value,
				ValidFor = rate.ValidFor
			}).ToList()
		};

		return Ok(response);
	}

	/// <summary>
	/// Get exchange rates for specified currencies using query parameters
	/// </summary>
	/// <param name="currencies">Comma-separated list of currency codes (e.g., "USD,EUR,JPY")</param>
	/// <param name="targetCurrency">The target currency (defaults to "CZK")</param>
	/// <param name="cancellationToken">Request cancellation token</param>
	/// <returns>Exchange rates for the requested currencies</returns>
	/// <response code="200">Returns the exchange rates for the requested currencies</response>
	/// <response code="400">If no currencies are provided or the request is invalid</response>
	/// <response code="500">If there was an internal server error</response>
	[HttpGet(ApiEndpoints.ExchangeRates.GetAllByQueryParams)]
    [SwaggerOperation(
        Summary = "Get exchange rates using GET request",
        Description = "Retrieves exchange rates for specified currencies using query parameters. Convenient for simple requests.",
        OperationId = "GetExchangeRatesGet")]
    [SwaggerResponse(200, "Exchange rates retrieved successfully", typeof(ExchangeRateResponse))
    ]
    [SwaggerResponse(400, "Invalid request - missing currency codes or format violation")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRatesQuery(
        [FromQuery, SwaggerParameter("Comma-separated currency codes (e.g., 'USD,EUR,JPY')", Required = true)] string currencies,
        [FromQuery, SwaggerParameter("Target currency code (defaults to 'CZK')")] string? targetCurrency = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencies))
        {
            throw new ArgumentException("Currency codes parameter is required");
        }

        if (!QueryCodesRegex.IsMatch(currencies))
        {
			throw new ArgumentException("Currency codes must be in XXX,YYY,ZZZ format with 3-letter codes");
		}

        var request = new ExchangeRateRequest
        {
            CurrencyCodes = GetCurrenctyCodesFromQueryParams(currencies),
            TargetCurrency = targetCurrency?.ToUpperInvariant()
        };

        return await GetExchangeRates(request, cancellationToken);
    }

    /// <summary>
    /// Get information about available exchange rate providers
    /// </summary>
    /// <returns>List of available currency providers</returns>
    /// <response code="200">Returns the list of available providers</response>
    [HttpGet(ApiEndpoints.Providers.GetAll)]
    [SwaggerOperation(
        Summary = "Get available exchange rate providers",
        Description = "Returns information about all available exchange rate providers and their supported currencies.",
        OperationId = "GetAvailableProviders")]
    [SwaggerResponse(200, "Available providers retrieved successfully")]
    public ActionResult<object> GetAvailableProviders()
    {
        var providers = new[]
        {
            new { 
                CurrencyCode = "CZK", 
                Name = "Czech National Bank", 
                Description = "Provides exchange rates with CZK as target currency",
                Endpoint = "https://api.cnb.cz/cnbapi/exrates/daily"
            },
			new {
				CurrencyCode = "USD",
				Name = "FED",
				Description = "Provides exchange rates with USD as target currency",
				Endpoint = "Mock data for testing purposes"
			}
		};

        return Ok(new { Providers = providers });
    }

	private async Task<IEnumerable<ExchangeRate>> GetExchangeRatesForCurrenciesAsync(string targetCurrency, IEnumerable<Currency> currencies, CancellationToken cancellationToken)
	{
		if (!currencies.Any())
		{
			_logger.LogWarning("No valid currency codes provided after filtering");
			throw new ArgumentException("No valid currency codes provided");
		}

		var exchangeRates = await _exchangeRateService.GetExchangeRatesAsync(targetCurrency, currencies, cancellationToken);

		return exchangeRates;
	}

	private List<string> GetCurrenctyCodesFromQueryParams(string currencies)
	{
		return currencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(c => c.Trim().ToUpperInvariant())
			.ToList();
	}
}