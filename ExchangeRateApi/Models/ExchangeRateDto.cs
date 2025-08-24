using Swashbuckle.AspNetCore.Annotations;

namespace ExchangeRateApi.Models;

/// <summary>
/// Exchange rate data transfer object
/// </summary>
[SwaggerSchema("Individual exchange rate information")]
public class ExchangeRateDto
{
    /// <summary>
    /// Source currency code (e.g., "USD")
    /// </summary>
    /// <example>USD</example>
    [SwaggerSchema("Source currency ISO 4217 code")]
    public string SourceCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Target currency code (e.g., "CZK")
    /// </summary>
    /// <example>CZK</example>
    [SwaggerSchema("Target currency ISO 4217 code")]
    public string TargetCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Exchange rate value
    /// </summary>
    /// <example>22.5000</example>
    [SwaggerSchema("Exchange rate value (how many target currency units for 1 source currency unit)")]
    public decimal Rate { get; set; }

    /// <summary>
    /// String representation of the exchange rate
    /// </summary>
    /// <example>USD/CZK=22.5000</example>
    [SwaggerSchema("Human-readable representation of the exchange rate")]
    public string DisplayValue => $"{SourceCurrency}/{TargetCurrency}={Rate:F4}";
}