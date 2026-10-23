using System.Net;
using System.Net.Sockets;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Infrastructure.Integrations.Providers;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Tests.Integrations.Providers;

/// <summary>
/// TAC-PF-FLOW-04 (TAC 117D) — provider outcome classification at the
/// Infrastructure webhook adapter seam. A mocked HTTP handler proves the
/// transport-to-semantic mapping the delivery mechanism relies on to classify
/// a retry: business rejection (4xx except 408/429), unambiguous connection-
/// phase failure (connection refused / DNS failure — the connection to the
/// provider was never established), and indeterminate outcomes (408, 429, 5xx,
/// timeout, connection reset/aborted, bare transport error) which MUST NOT be
/// auto re-fired because the provider may have processed the call. The adapter
/// performs exactly one HTTP attempt — it never retries internally, so the
/// durable delivery mechanism stays the single retry owner.
/// </summary>
public class N8nClientTests
{
    private static N8nClient ClientWith(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    {
        var http = new HttpClient(new StubHandler(send));
        var options = Microsoft.Extensions.Options.Options.Create(new N8nOptions { WebhookBasePath = "https://n8n.test/webhook" });
        return new N8nClient(http, options);
    }

    [Fact]
    public async Task AcceptedWebhook_ReturnsSucceeded()
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.Succeeded);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]   // 408 request timeout
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task IndeterminateStatus_ReturnsUnknownOutcome(HttpStatusCode status)
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(status)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            $"{(int)status} is indeterminate: the provider may or may not have processed the call");
        result.Error.Should().Contain(((int)status).ToString());
    }

    [Fact]
    public async Task RateLimitStatus_NotRetryable_WithoutGuaranteedPreExecutionRejection()
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            "429 must NOT be auto re-fired unless the provider/gateway contract guarantees rejection before execution");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]      // 400 provider rejected the call
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task RejectionStatus_ReturnsTerminalFailure(HttpStatusCode status)
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(status)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.TerminalFailure,
            $"{(int)status} is a business rejection: a retry of the same request cannot succeed");
    }

    [Fact]
    public async Task RequestTimeout_ReturnsUnknownOutcome_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new TaskCanceledException("the request timed out before a response arrived");
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            "a timeout is not proof the provider did not process the call");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task ConnectionRefused_ReturnsRetryableFailure_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException(
                "connection refused",
                new SocketException((int)SocketError.ConnectionRefused));
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.RetryableFailure,
            "a connection refused the request never reached the provider — safe to retry");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task NameResolutionFailure_ReturnsRetryableFailure_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException(
                "no such host",
                new SocketException((int)SocketError.HostNotFound));
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.RetryableFailure,
            "a DNS failure proves the request never reached the provider — safe to retry");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task ConnectionAborted_ReturnsUnknownOutcome_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException(
                "connection aborted",
                new SocketException((int)SocketError.ConnectionAborted));
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            "a connection abort does not prove the connection was never established — indeterminate");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task ConnectionReset_ReturnsUnknownOutcome_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException(
                "connection reset by peer",
                new SocketException((int)SocketError.ConnectionReset));
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            "a connection reset may occur after the provider received the call — indeterminate");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task BareTransportError_ReturnsUnknownOutcome_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException("n8n unreachable");
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.UnknownOutcome,
            "a transport error without a proven connection-phase cause is indeterminate");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    [Fact]
    public async Task CallerCancellation_PropagatesToCaller_NotClassifiedAsUnknownOutcome()
    {
        var client = ClientWith((_, _) => throw new TaskCanceledException("the request timed out"));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var consume = () => client.TriggerWebhookAsync("card-assigned", "{}", cts.Token);

        await consume.Should().ThrowAsync<OperationCanceledException>(
            "the deliverer's own cancellation must propagate, not be disguised as an unknown provider outcome");
    }

    [Theory]
    [InlineData("../../rest/api")]
    [InlineData("a/../b")]
    [InlineData("..")]
    [InlineData("%2e%2e/x")]
    [InlineData("hook?x=1")]
    [InlineData("hook#f")]
    [InlineData("a\\b")]
    [InlineData("/leading")]
    [InlineData("http://evil.test/x")]
    [InlineData("host:5678/x")]
    [InlineData("")]
    public async Task UnsafeWebhookPath_FailsClosed_WithoutAnyHttpCall(string path)
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var result = await client.TriggerWebhookAsync(path, "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.TerminalFailure);
        attempts.Should().Be(0, "a path outside the allow-list must never produce a request");
    }

    [Fact]
    public async Task ValidNestedPath_IsAppendedVerbatimToTheBase()
    {
        Uri? seen = null;
        var client = ClientWith((request, _) =>
        {
            seen = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        await client.TriggerWebhookAsync("team/hooks/a-1", "{}");

        seen!.AbsoluteUri.Should().Be("https://n8n.test/webhook/team/hooks/a-1");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task ProviderResponseBody_IsNeverReturnedInTheError(HttpStatusCode status)
    {
        const string secret = "SECRET-INTERNAL-STACK-TRACE db-password=hunter2 at n8n.internal:5678";
        var client = ClientWith((_, _) => Task.FromResult(
            new HttpResponseMessage(status) { Content = new StringContent(secret) }));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Error.Should().NotBeNull().And.Contain(((int)status).ToString());
        result.Error.Should().NotContain("SECRET").And.NotContain("hunter2").And.NotContain("n8n.internal");
    }

    [Fact]
    public async Task TransportException_MessageIsNeverReturnedInTheError()
    {
        var client = ClientWith((_, _) => throw new HttpRequestException(
            "Connection refused (n8n.internal:5678)",
            new SocketException((int)SocketError.ConnectionRefused)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Error.Should().NotContain("n8n.internal");
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}