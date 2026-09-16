using System.Net;
using System.Net.Http;
using Notrelix.Application.Features.Integrations.Public.Commands;
using Notrelix.Infrastructure.Integrations.Providers;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure.Tests.Integrations.Providers;

/// <summary>
/// TAC-PF-FLOW-04 (TAC 117D) — provider outcome classification at the
/// Infrastructure webhook adapter seam. A mocked HTTP handler proves the
/// transport-to-semantic mapping the delivery mechanism relies on to classify
/// a retry: business rejection (4xx), technical transient (connection failure),
/// rate-limit / timeout (429 / 408 / 5xx), and unknown outcome (a request that
/// times out may still have been processed). The adapter performs exactly one
/// HTTP attempt — it never retries internally, so the durable delivery mechanism
/// stays the single retry owner.
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
    [InlineData(HttpStatusCode.TooManyRequests)]  // 429 rate limit
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TransientProviderStatus_ReturnsRetryableFailure(HttpStatusCode status)
    {
        var client = ClientWith((_, _) => Task.FromResult(new HttpResponseMessage(status)));

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.RetryableFailure,
            $"{(int)status} is a transient transport signal a later attempt may clear");
        result.Error.Should().Contain(((int)status).ToString());
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
    public async Task ConnectionFailure_ReturnsRetryableFailure_AndMakesExactlyOneAttempt()
    {
        var attempts = 0;
        var client = ClientWith((_, _) =>
        {
            attempts++;
            throw new HttpRequestException("n8n unreachable");
        });

        var result = await client.TriggerWebhookAsync("card-assigned", "{}");

        result.Outcome.Should().Be(N8nWebhookOutcome.RetryableFailure,
            "a transport/connection failure is transient and may be retried");
        attempts.Should().Be(1,
            "the adapter makes exactly one attempt; retry ownership stays with the delivery mechanism");
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
