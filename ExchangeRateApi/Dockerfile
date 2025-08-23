FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy just the project file first
COPY ["ExchangeRateApi/ExchangeRateApi.csproj", "ExchangeRateApi/"]
COPY ["ExchangeRateProviders/ExchangeRateProviders.csproj", "ExchangeRateProviders/"]
RUN dotnet restore "ExchangeRateApi/ExchangeRateApi.csproj"

# Copy everything else
COPY . .

WORKDIR "/src/ExchangeRateApi"
RUN dotnet publish "ExchangeRateApi.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8085
EXPOSE 8085
ENTRYPOINT ["dotnet", "ExchangeRateApi.dll"]
