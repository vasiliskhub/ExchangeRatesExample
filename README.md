# Exchange Rate API

This API provides exchange rates using various exchange rate providers. 
Currently supports Czech National Bank (CZK) as the base currency provider.

Deployed via cicd pipeline on hetzner 

http://128.140.72.56:18080/swagger/index.html

## ?? API Documentation

You can access the interactive Swagger UI documentation at:
- **Swagger UI**: `/swagger`
- **OpenAPI JSON**: `/swagger/v1/swagger.json`

The Swagger UI provides:
- Interactive API testing
- Detailed endpoint documentation
- Request/response examples
- Schema definitions
- Authentication details (when applicable)

## Endpoints

### POST /api/exchangerate/rates
Get exchange rates for specified currencies using a JSON request body.

**Request Body:**{
  "currencyCodes": ["USD", "EUR", "JPY"],
  "baseCurrency": "CZK"
}
**Response:**
{
  "baseCurrency": "CZK",
  "rates": [
    {
      "fromCurrency": "USD",
      "toCurrency": "CZK",
      "rate": 22.5000,
      "displayValue": "USD/CZK=22.5000"
    },
    {
      "fromCurrency": "EUR", 
      "toCurrency": "CZK",
      "rate": 24.0000,
      "displayValue": "EUR/CZK=24.0000"
    }
  ],
  "retrievedAt": "2024-01-15T10:30:00Z"
}
### GET /api/exchangerate/rates
Get exchange rates using query parameters.

**Parameters:**
- `currencies` (required): Comma-separated currency codes (e.g., "USD,EUR,JPY")
- `baseCurrency` (optional): Base currency code (defaults to "CZK")

**Example:** `/api/exchangerate/rates?currencies=USD,EUR,JPY&baseCurrency=CZK`

### GET /api/exchangerate/providers
Get information about available exchange rate providers.

**Response:**{
  "providers": [
    {
      "currencyCode": "CZK",
      "name": "Czech National Bank",
      "description": "Provides exchange rates with CZK as base currency",
      "endpoint": "https://api.cnb.cz/cnbapi/exrates/daily"
    }
  ]
}
## Examples

### Using Swagger UI (Recommended)
1. Start the API in development mode
2. Navigate to `/swagger` in your browser
3. Use the interactive interface to test endpoints
4. View detailed documentation and examples

### Using curl
# POST request with JSON body
curl -X POST "https://localhost:7000/api/exchangerate/rates" \
  -H "Content-Type: application/json" \
  -d '{
    "currencyCodes": ["USD", "EUR", "JPY"],
    "baseCurrency": "CZK"
  }'

# GET request with query parameters  
curl "https://localhost:7000/api/exchangerate/rates?currencies=USD,EUR,JPY"

# Get available providers
curl "https://localhost:7000/api/exchangerate/providers"

### Using C#
var client = new HttpClient();
var request = new
{
    CurrencyCodes = new[] { "USD", "EUR", "JPY" },
    BaseCurrency = "CZK"
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync("https://localhost:7000/api/exchangerate/rates", content);
var responseJson = await response.Content.ReadAsStringAsync();

## ?? Getting Started

1. **Clone and build the project**
2. **Run the API**: `dotnet run --project ExchangeRateApi`
3. **Access Swagger UI**: Navigate to `https://localhost:7000/swagger`
4. **Test endpoints**: Use the interactive Swagger interface or any HTTP client

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK`: Successful request
- `400 Bad Request`: Invalid request (missing currencies, etc.)
- `500 Internal Server Error`: Server error

Error responses include descriptive messages to help with debugging.

## Features

- ? **Interactive Documentation**: Swagger UI with examples and testing capabilities
- ? **Flexible Input**: POST (JSON) and GET (query params) request methods
- ? **Comprehensive Validation**: Input validation with clear error messages
- ? **Caching**: Built-in caching for improved performance
- ? **Logging**: Structured logging throughout the application
- ? **Error Handling**: Robust error handling with appropriate HTTP status codes