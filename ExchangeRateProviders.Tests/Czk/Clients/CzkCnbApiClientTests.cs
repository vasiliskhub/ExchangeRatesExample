using ExchangeRateProviders.Czk.Clients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Polly;
using System.Net;
using System.Text;

namespace ExchangeRateProviders.Tests.Czk.Clients;

[TestFixture]
public class CzkCnbApiClientTests
{
	private const string TwoRatesJson = """
        { "rates": [
            { "code":"EUR", "rate":25.10 },
            { "code":"USD", "rate":22.90 }
        ]}
        """;
	private const string OneRateJson = """{ "rates": [ { "code":"EUR", "rate":25.10 } ] }""";
	private const string EmptyRatesJson = """{ "rates": [] }""";

	private static IAsyncPolicy<HttpResponseMessage> ZeroBackoffRetry(int retries = 3) =>
		Policy<HttpResponseMessage>
			.Handle<HttpRequestException>()
			.OrResult(r => (int)r.StatusCode >= 500 || (int)r.StatusCode == 429)
			.WaitAndRetryAsync(retries, _ => TimeSpan.Zero);

	[Test]
	public async Task GetDailyRatesRawAsync_ReturnsList_On200()
	{
		// Arrange
		var handler = new SequenceHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(TwoRatesJson, Encoding.UTF8, "application/json") });
		var (client, logger) = CreateClientWithHandler(handler);

		// Act
		var result = await client.GetDailyRatesRawAsync();

		// Assert
		Assert.That(result, Has.Count.EqualTo(2));
		Assert.That(handler.CallCount, Is.EqualTo(1));
		Assert.That(logger.JoinedMessages, Does.Contain("CNB returned 2 raw rates"));
	}

	[Test]
	public async Task GetDailyRatesRawAsync_EmptyRates_LogsWarning()
	{
		// Arrange
		var handler = new SequenceHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(EmptyRatesJson, Encoding.UTF8, "application/json") });
		var (client, logger) = CreateClientWithHandler(handler);

		// Act
		var result = await client.GetDailyRatesRawAsync();

		// Assert
		Assert.That(result, Is.Empty);
		Assert.That(logger.JoinedMessages, Does.Contain("response empty"));
	}

	[Test]
	public async Task GetDailyRatesRawAsync_RetriesOnServerError_ThenSucceeds()
	{
		// Arrange (500 then success)
		var handler = new SequenceHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.InternalServerError),
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(OneRateJson, Encoding.UTF8, "application/json") });
		var (client, _) = CreateClientWithHandler(handler);

		// Act
		var result = await client.GetDailyRatesRawAsync();

		// Assert
		Assert.That(result, Has.Count.EqualTo(1));
		Assert.That(handler.CallCount, Is.EqualTo(2)); // 1 retry
	}

	[Test]
	public void GetDailyRatesRawAsync_TooManyFailures_Throws()
	{
		// Arrange (exceed retry attempts all 429)
		var handler = new SequenceHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.TooManyRequests),
			new HttpResponseMessage(HttpStatusCode.TooManyRequests),
			new HttpResponseMessage(HttpStatusCode.TooManyRequests),
			new HttpResponseMessage(HttpStatusCode.TooManyRequests));
		var (client, _) = CreateClientWithHandler(handler);

		// Act & Assert
		Assert.ThrowsAsync<HttpRequestException>(() => client.GetDailyRatesRawAsync());
		Assert.That(handler.CallCount, Is.EqualTo(4));
	}

	[Test]
	public async Task GetDailyRatesRawAsync_RespectsCancellationToken()
	{
		// Arrange (long delay to allow cancellation)
		var handler = new FakeHttpMessageHandler(async (_, ct) =>
		{
			await Task.Delay(TimeSpan.FromSeconds(5), ct);
			return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(EmptyRatesJson, Encoding.UTF8, "application/json") };
		});
		var (client, _) = CreateClientWithHandler(handler);
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));

		// Act & Assert
		Assert.That(async () => await client.GetDailyRatesRawAsync(cts.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(handler.CallCount, Is.EqualTo(1));
	}

	[Test]
	public async Task LogsInformation_AtLeastOnce_WithNSubstituteILogger()
	{
		// Arrange
		var handler = new SequenceHttpMessageHandler(
			new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(EmptyRatesJson, Encoding.UTF8, "application/json") });
		var http = new HttpClient(handler);
		var substituteLogger = Substitute.For<ILogger<CzkCnbApiClient>>();
		var client = CreateClient(http, substituteLogger);

		// Act
		await client.GetDailyRatesRawAsync();

		// Assert
		substituteLogger.ReceivedWithAnyArgs().Log(default, default, default!, default, default!);
	}

	private static (CzkCnbApiClient client, ListLogger<CzkCnbApiClient> logger) CreateClientWithHandler(HttpMessageHandler handler)
	{
		var http = new HttpClient(handler);
		var logger = new ListLogger<CzkCnbApiClient>();
		var client = CreateClient(http, logger);
		return (client, logger);
	}

	private static CzkCnbApiClient CreateClient(HttpClient http, ILogger<CzkCnbApiClient> logger)
	{
		try
		{
			return (CzkCnbApiClient)Activator.CreateInstance(
				typeof(CzkCnbApiClient),
				args: new object?[] { http, logger, ZeroBackoffRetry() })!;
		}
		catch (MissingMethodException)
		{
			return (CzkCnbApiClient)Activator.CreateInstance(
				typeof(CzkCnbApiClient),
				args: new object?[] { http, logger })!;
		}
	}

	private sealed class ListLogger<T> : ILogger<T>
	{
		private readonly List<(LogLevel Level, string Message)> _entries = new();
		public string JoinedMessages => string.Join(" | ", _entries.ConvertAll(e => $"[{e.Level}] {e.Message}"));

		public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			var message = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
			_entries.Add((logLevel, message));
		}

		private sealed class NullScope : IDisposable { public static readonly NullScope Instance = new(); public void Dispose() { } }
	}

	private sealed class FakeHttpMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
		public int CallCount { get; private set; }

		public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			return _responder(request, cancellationToken);
		}
	}

	private sealed class SequenceHttpMessageHandler : HttpMessageHandler
	{
		private readonly Queue<HttpResponseMessage> _responses = new();
		public int CallCount { get; private set; }

		public SequenceHttpMessageHandler(params HttpResponseMessage[] responses)
		{
			foreach (var r in responses) _responses.Enqueue(r);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			CallCount++;
			var next = _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.OK);
			return Task.FromResult(next);
		}
	}
}