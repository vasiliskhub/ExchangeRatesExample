namespace ExchangeRateApi;

public static class ApiEndpoints
{
	public const string ApiVersion = "v1";
	private const string ApiBase = $"{ApiVersion}/api";

	public static class ExchangeRates
	{
		public const string Base = $"{ApiBase}/exchange-rates";
		public const string RatesPost = $"{Base}/rates"; // POST
		public const string RatesGet = $"{Base}/rates";  // GET (query version)
		public const string Providers = $"{Base}/providers"; // GET providers
	}
}
