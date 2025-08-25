using ExchangeRateApi;
using ExchangeRateApi.Models;
using ExchangeRateApi.Models.Validators;
using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Clients;
using ExchangeRateProviders.Usd;
using FluentValidation;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IValidator<ExchangeRateRequest>, ExchangeRateRequestValidator>();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Exchange Rate API",
		Version = ApiEndpoints.ApiVersion,
		Description = "API for retrieving exchange rates from various providers",
		Contact = new OpenApiContact
		{
			Name = "Exchange Rate API",
			Email = "support@exchangerate.api"
		}
	});

	// Include XML comments for better documentation
	var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
	var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
	if (File.Exists(xmlPath))
	{
		c.IncludeXmlComments(xmlPath);
	}

	// Add example schemas
	c.EnableAnnotations();
});

// Add logging
builder.Logging.AddConsole();

// Add Exchange Rate Provider services
ConfigureExchangeRateServices(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", $"Exchange Rate API {ApiEndpoints.ApiVersion}");
		c.RoutePrefix = "swagger";
		c.DocumentTitle = "Exchange Rate API Documentation";
		c.DefaultModelsExpandDepth(2);
		c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
		c.DisplayRequestDuration();
		c.EnableDeepLinking();
		c.EnableFilter();
		c.ShowExtensions();
	});

app.UseAuthorization();

app.MapControllers();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Exchange Rate API started successfully");
logger.LogInformation("Swagger UI available at: /swagger");

app.Run();

static void ConfigureExchangeRateServices(IServiceCollection services)
{
	services.AddFusionCache();

	//Register HtpClients for API clients
	services.AddHttpClient<ICzkCnbApiClient, CzkCnbApiClient>();

	//Register exchange rate providers and factory
	services.AddSingleton<IExchangeRateDataProvider, CzkExchangeRateDataProvider>();
	services.AddSingleton<IExchangeRateDataProvider, UsdExchangeRateDataProvider>();
	services.AddSingleton<IExchangeRateDataProviderFactory, ExchangeRateDataProviderFactory>();

	//Register the exchange rate service
	services.AddSingleton<IExchangeRateService, ExchangeRateService>();
}