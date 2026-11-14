using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Integrations.Calendar.Commands.HandleCalendarWebhook;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// UC-INT-G01: the anonymous calendar webhook route is limited per IP under the shipped
/// <c>WebhookIntakeByIp</c> tier (30 per minute). The limit is answered before the handler runs, so a flood
/// never reaches verification or processing.
/// </summary>
public sealed class WebhookIntakeRateLimitTests
{
    private const string WebhookPath = "/api/v1/integrations/calendar/webhooks/google/rate-limit-path-abc123";
    private const int ShippedPermitLimit = 30;

    [Fact]
    public async Task WebhookIntake_ThirtyFirstDeliveryFromOneAddress_IsRefusedWith429_BeforeTheHandler()
    {
        var handlerCalls = 0;
        await using var factory = new ShippedRateLimitsApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
            services.AddScoped<IRequestHandler<HandleCalendarWebhookCommand, Result>>(_ =>
            {
                var handler = new Mock<IRequestHandler<HandleCalendarWebhookCommand, Result>>();
                handler.Setup(h => h.Handle(It.IsAny<HandleCalendarWebhookCommand>(), It.IsAny<CancellationToken>()))
                    .Callback(() => handlerCalls++)
                    .ReturnsAsync(Result.Failure("integrations.webhook.rejected"));
                return handler.Object;
            });
        }));
        var client = factory.CreateClient();

        for (var attempt = 1; attempt <= ShippedPermitLimit; attempt++)
        {
            var response = await PostDeliveryAsync(client);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"delivery {attempt} is within the shipped limit");
        }

        var limited = await PostDeliveryAsync(client);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
        limited.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errorCode").GetString().Should().Be("rate_limit.exceeded");
        handlerCalls.Should().Be(ShippedPermitLimit, "the limited delivery never reaches verification or processing");
    }

    private static Task<HttpResponseMessage> PostDeliveryAsync(HttpClient client)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookPath)
        {
            Content = new StringContent("""{"kind":"calendar#event"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Calendar-Signature", "forged-signature");
        request.Headers.Add("X-Calendar-Timestamp", "1757400000");
        return client.SendAsync(request);
    }
}
