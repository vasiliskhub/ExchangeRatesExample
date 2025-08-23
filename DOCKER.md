# Docker Setup for Exchange Rate API

This directory contains Docker and Docker Compose configurations for the Exchange Rate API.

## Quick Start

### Production Setup# Build and start the services
docker-compose up -d

# View logs
docker-compose logs -f exchangerate-api

# Stop services
docker-compose down
### Development Setup# Use the development compose file
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f exchangerate-api

# Stop services
docker-compose -f docker-compose.dev.yml down
## Services

### ExchangeRate API
- **Port**: 8080
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/swagger (using Swagger endpoint)
- **Caching**: In-memory FusionCache (5-minute TTL)

## Configuration

### Docker Compose Files

- `docker-compose.yml`: Production setup with single API service
- `docker-compose.dev.yml`: Development setup with enhanced logging and volume mounting

## Caching

### In-Memory Caching with FusionCache
The application uses **ZiggyCreatures.FusionCache** for in-memory caching:
- **Cache Duration**: 5 minutes for exchange rate data
- **Cache Key**: "CnbDailyRates"
- **Benefits**: Fast access, automatic expiration, no external dependencies
- **Persistence**: Cache is reset when container restarts (which is fine for 5-minute TTL data)

### Cache Behavior
- First request to exchange rates: Fetches from Czech National Bank API
- Subsequent requests (within 5 minutes): Served from in-memory cache
- After 5 minutes: Cache expires, next request fetches fresh data

## Building

### Manual Docker Build# Build the API image
docker build -f ExchangeRateApi/Dockerfile -t exchangerate-api:latest .

# Run the container
docker run -p 8080:8080 exchangerate-api:latest
### Docker Compose Build# Build and start services
docker-compose up --build -d

# Rebuild specific service
docker-compose build exchangerate-api
## Data Persistence

### Logs (Development)
Application logs are mounted to `./ExchangeRateApi/logs` directory in development mode.

### Cache Data
No persistence needed - in-memory cache with 5-minute TTL is perfect for exchange rate data that changes daily.

## Networking

The API service uses the `exchangerate-network` bridge network for isolation from other Docker applications.

## Health Checks

Health check is configured to test the Swagger endpoint availability:
- **Endpoint**: `http://localhost:8080/swagger`
- **Interval**: Every 30 seconds
- **Timeout**: 10 seconds
- **Retries**: 3 attempts
- **Start Period**: 40 seconds

## Troubleshooting

### Common Issues

1. **Port Conflicts**# Check if port 8080 is in use
netstat -tulpn | grep :8080
2. **Container Logs**# View API logs
   docker-compose logs exchangerate-api
3. **Cache Issues**
   - Cache is in-memory only
   - Restarting container clears cache (normal behavior)
   - Cache automatically expires after 5 minutes

4. **Rebuild Clean**# Clean rebuild
docker-compose down
docker-compose build --no-cache
docker-compose up -d
### Memory and Performance

For production deployments, consider:
- Setting memory limits for containers
- Monitoring memory usage of in-memory cache
- Container resource limits based on your load

## Security Notes

- API exposes Swagger UI - consider disabling in production by setting `ASPNETCORE_ENVIRONMENT=Production` without development middleware
- Use HTTPS in production environments
- No external cache dependencies simplifies security surface