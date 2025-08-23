using ExchangeRateProviders;
using ExchangeRateProviders.Core;
using ExchangeRateProviders.Czk;
using ExchangeRateProviders.Czk.Clients;
using ExchangeRateProviders.Czk.Mappers;
using ExchangeRateProviders.Czk.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Exchange Rate API",
        Version = "v1",
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Rate API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Exchange Rate API Documentation";
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

app.UseHttpsRedirection();

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
    services.AddHttpClient<ICzkCnbClient, CzkCnbApiClient>();
    services.AddSingleton<ICzkExchangeRatesMapper, CzkExchangeRateMapper>();
    services.AddTransient<IExchangeRateDataProvider, CzkExchangeRateDataProviderSevice>();
    services.AddSingleton<IExchangeRateProvider, CzkExchangeRateProvider>();
    services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();
}

// Make Program class accessible for testing
public partial class Program { }
