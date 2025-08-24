using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Core.Model;
using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Clients;
using ExchangeRateProviders.Czk.Services;
using ExchangeRateProviders.Usd;
using ExchangeRateProviders.Usd.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

const string ExchangeRateUsdProviderCurrencyCode = "USD";
const string ExchangeRateCzkProviderCurrencyCode = "CZK";

var currencies = new List<Currency>
{
    new("USD"),
    new("EUR"),
    new("CZK"),
    new("JPY"),
    new("KES"),
    new("RUB"),
    new("THB"),
    new("TRY"),
    new("XYZ")
};

var builder = Host.CreateApplicationBuilder(args);
ConfigureServices(builder.Services);

using var host = builder.Build();

try
{
    var factory = host.Services.GetRequiredService<IExchangeRateProviderFactory>();

    Console.WriteLine("=== CZK Provider (rates TO CZK) ===");
    var czkProvider = factory.GetProvider(ExchangeRateCzkProviderCurrencyCode);
    var ratesCzk = await czkProvider.GetExchangeRatesAsync(currencies, CancellationToken.None);

    Console.WriteLine($"Successfully retrieved {ratesCzk.Count()} CZK exchange rates:");
    foreach (var rate in ratesCzk)
    {
        Console.WriteLine(rate.ToString());
    }

    Console.WriteLine("\n=== USD Provider (rates TO USD) ===");
    var usdProvider = factory.GetProvider(ExchangeRateUsdProviderCurrencyCode);
    var ratesUsd = await usdProvider.GetExchangeRatesAsync(currencies, CancellationToken.None);

    Console.WriteLine($"Successfully retrieved {ratesUsd.Count()} USD exchange rates:");
    foreach (var rate in ratesUsd)
    {
        Console.WriteLine(rate.ToString());
    }
}
catch (Exception e)
{
    Console.WriteLine($"Could not retrieve exchange rates: '{e.Message}'");
}

return;

static void ConfigureServices(IServiceCollection services)
{
    // FusionCache for CZK provider
    services.AddFusionCache();
    
    // CZK Provider dependencies
    services.AddHttpClient<ICzkCnbApiClient, CzkCnbApiClient>();
    services.AddSingleton<ICzkExchangeRateDataProvider, CzkExchangeRateDataProviderSevice>();

	// USD Provider dependencies
	services.AddSingleton<IUsdExchangeRateDataProvider, UsdExchangeRateDataProviderService>();

	// Register both providers
	services.AddSingleton<IExchangeRateProvider, CzkExchangeRateProvider>();
    services.AddSingleton<IExchangeRateProvider, UsdExchangeRateProvider>();
    
    // Factory to resolve providers
    services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
}