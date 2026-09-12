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
/// HTTP contract for the anonymous, signature-authenticated provider webhook:
/// with CSRF enabled the route-declared signature-auth metadata (not a blanket
/// skip) keeps the double-submit gate out of the intake path, and verification
/// failure surfaces as the endpoint's real 401 — never a CSRF 403.
/// </summary>
public class CalendarWebhookHttpContractTests
{
    private const string WebhookPath = "/api/v1/integrations/calendar/webhooks/google";

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
}
