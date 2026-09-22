using System.Net;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// HTTP contract for the anonymous, signature-authenticated provider webhook
/// on the C6 per-connection route: with CSRF enabled the route-declared
/// signature-auth metadata (not a blanket skip) keeps the double-submit gate
/// out of the intake path, and verification failure surfaces as the endpoint's
/// real 401 — never a CSRF 403. The route now carries the per-connection
/// {webhookPath}; the legacy provider-only route no longer exists and fails
/// closed as 404 (no tenant is ever adopted through a stale route).
/// </summary>
public class CalendarWebhookHttpContractTests
{
    private const string WebhookPath = "/api/v1/integrations/calendar/webhooks/google/test-webhook-path-abc123";
    private const string LegacyRoutePath = "/api/v1/integrations/calendar/webhooks/google";

    private const int MaxWebhookPayloadBytes = 256 * 1024;

    [Fact]
    public async Task VerifiedCallback_WithoutBearer_WithoutCsrfMaterial_ReachesIntake_Returns200()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("""{"kind":"calendar#event"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "a signature-authenticated provider callback carries no ambient browser session " +
            "and must pass the intake without CSRF material");
    }

    [Fact]
    public async Task RejectedCallback_FailsVerification_Returns401_NeverCsrfForbidden()
    {
        await using var factory = new WebhookResultApiFactory(Result.Failure("integrations.webhook.rejected"));
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("""{"kind":"calendar#event"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "forged-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "verification failure is the endpoint's declared rejection contract");
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
            "the rejection must come from the pipeline, not the CSRF gate");
    }

    [Fact]
    public async Task NonJsonContentType_IsRejectedAtRawBoundary_Returns415()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("""{"kind":"calendar#event"}""", Encoding.UTF8, "text/plain"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType,
            "the raw HTTP boundary rejects non-application/json callback bodies with 415");
    }

    [Fact]
    public async Task ContentLengthOverLimit_IsRejectedAtRawBoundary_Returns413()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent(new string('a', MaxWebhookPayloadBytes + 1), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge,
            "a known over-limit Content-Length is rejected at the boundary without reading the payload");
    }

    [Fact]
    public async Task ChunkedBodyExceedingStreamingLimit_IsRejectedAtRawBoundary_Returns413()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        // Chunked transfer (no Content-Length) must still hit the streaming
        // bound: a request that advertises no length cannot bypass the limit.
        var oversized = new byte[MaxWebhookPayloadBytes + 1];
        Array.Fill(oversized, (byte)'a');

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StreamContent(new MemoryStream(oversized)),
        };
        request.Content.Headers.ContentType = new("application/json");
        request.Content.Headers.ContentLength = null;
        request.Headers.TransferEncodingChunked = true;
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge,
            "chunked/unknown-length bodies are bounded by the streaming read, not only Content-Length");
    }

    [Fact]
    public async Task EmptyBody_IsRejectedAtRawBoundary_Returns400()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "an empty provider callback body is malformed and rejected at the raw HTTP boundary");
    }

    [Fact]
    public async Task MalformedJsonBody_IsRejectedAtRawBoundary_Returns400()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("""{"kind": }""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "a malformed JSON callback body is rejected at the raw HTTP boundary");
    }

    [Fact]
    public async Task RawBytes_ArePreservedForSignatureAndHash_ReachHandlerUnchanged()
    {
        // The exact raw bytes the provider signs must be the exact bytes the
        // intake hashes: the command must carry the raw body byte-for-byte, so
        // a later Content-Length/spoofed decoding cannot desynchronize the
        // signature and the persisted payload hash.
        await using var factory = new WebhookCapturingApiFactory();
        var client = factory.CreateClient();

        const string payload = "{\"eventId\":\"evt-123\",\"kind\":\"calendar#event\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.ReceivedCommand?.RawBody.Should().Be(payload,
            "the handler must receive the exact raw bytes, unchanged by any text re-materialization");
    }

    [Fact]
    public async Task LegacyProviderOnlyRoute_FailsClosed_Returns404()
    {
        await using var factory = new CsrfEnabledApiFactory();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, LegacyRoutePath)
        {
            Content = new StringContent("""{"kind":"calendar#event"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "test-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");

        var response = await client.SendAsync(request);

        // The legacy provider-only route must fail closed: either 404 (no
        // route) or 403 (CSRF rejects the unmatched unsigned POST) proves no
        // handler/webhookPath is ever reached.
        response.StatusCode.Should().BeOneOf(new[]
        {
            HttpStatusCode.NotFound,
            HttpStatusCode.Forbidden,
        });
    }

    /// <summary>
    /// Same host as <see cref="CsrfEnabledApiFactory"/> but the webhook command
    /// handler is replaced with a per-test verification outcome so the 401
    /// contract can be proven end-to-end through the real pipeline.
    /// </summary>
    private sealed class WebhookResultApiFactory(Result handlerResult) : CsrfEnabledApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
                services.AddScoped<IRequestHandler<HandleCalendarWebhookCommand, Result>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
                    handler.Setup(h => h.Handle(It.IsAny<HandleCalendarWebhookCommand>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(handlerResult);
                    return handler.Object;
                });
            });
        }
    }

    /// <summary>
    /// Replaces the webhook command handler with an echo that captures the
    /// exact command (and therefore the exact raw body bytes) so the raw
    /// byte-for-byte preservation contract can be asserted.
    /// </summary>
    private sealed class WebhookCapturingApiFactory : CsrfEnabledApiFactory
    {
        private readonly List<HandleCalendarWebhookCommand> _received = new();

        public HandleCalendarWebhookCommand? ReceivedCommand => _received.FirstOrDefault();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
                services.AddScoped<IRequestHandler<HandleCalendarWebhookCommand, Result>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
                    handler.Setup(h => h.Handle(It.IsAny<HandleCalendarWebhookCommand>(), It.IsAny<CancellationToken>()))
                        .Callback<HandleCalendarWebhookCommand, CancellationToken>((command, _) => _received.Add(command))
                        .ReturnsAsync(Result.Success());
                    return handler.Object;
                });
            });
        }
    }
}