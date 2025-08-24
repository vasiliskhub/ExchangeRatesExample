using Swashbuckle.AspNetCore.Annotations;

namespace ExchangeRateApi.Models;

/// <summary>
/// Response model for exchange rates
/// </summary>
[SwaggerSchema("Response containing exchange rates and metadata")]
public class ExchangeRateResponse
{
    /// <summary>
    /// The base currency used for the exchange rates
    /// </summary>
    /// <example>CZK</example>
    [SwaggerSchema("Target currency code used for all exchange rates")]
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// List of exchange rates
    /// </summary>
    [SwaggerSchema("Array of exchange rate objects")]
    public List<ExchangeRateDto> Rates { get; set; } = new();

    /// <summary>
    /// Timestamp when the rates were retrieved
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    [SwaggerSchema("UTC timestamp when the rates were retrieved")]
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
}