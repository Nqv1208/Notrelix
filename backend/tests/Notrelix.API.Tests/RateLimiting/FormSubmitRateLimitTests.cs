using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitForm;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// Anti-abuse of form submission under the shipped <c>FormSubmitByIpAndForm</c> tier (10 per minute for one
/// address and one form): the eleventh submission is a 429 with a Vietnamese message before the handler runs,
/// another form of the same address still passes, and a filled honeypot reaches the command.
/// </summary>
public sealed class FormSubmitRateLimitTests
{
    private static readonly Guid AccountId = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private const int ShippedPermitLimit = 10;

    [Fact]
    public async Task Submit_EleventhFromOneAddressToOneForm_Is429WithVietnameseMessage_AndOtherFormStillPasses()
    {
        var received = new List<SubmitFormCommand>();
        await using var factory = new ShippedRateLimitsApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IResourceLocator>();
            services.AddScoped<IResourceLocator>(_ =>
            {
                var locator = new Mock<IResourceLocator>();
                locator.Setup(x => x.LocateAsync(It.IsAny<ResourceRef>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((ResourceRef resource, Guid _, CancellationToken _) =>
                        new ResourceLocation(resource.Kind, resource.ResourceId, AccountId, AccountId));
                return locator.Object;
            });
            services.RemoveAll<IRequestHandler<SubmitFormCommand, Result<Guid>>>();
            services.AddScoped<IRequestHandler<SubmitFormCommand, Result<Guid>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<SubmitFormCommand, Result<Guid>>>();
                handler.Setup(h => h.Handle(It.IsAny<SubmitFormCommand>(), It.IsAny<CancellationToken>()))
                    .Callback<SubmitFormCommand, CancellationToken>((c, _) => received.Add(c))
                    .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));
                return handler.Object;
            });
        }));
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        var formId = Guid.NewGuid();

        for (var attempt = 1; attempt <= ShippedPermitLimit; attempt++)
        {
            var ok = await PostAsync(client, formId, """{"answers":{},"website":"http://bot.example"}""");
            ok.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"submission {attempt} is within the limit");
        }

        var limited = await PostAsync(client, formId, """{"answers":{}}""");
        var other = await PostAsync(client, Guid.NewGuid(), """{"answers":{}}""");

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Be("Bạn đã gửi quá nhiều phản hồi cho biểu mẫu này. Vui lòng thử lại sau ít phút.");
        other.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, "the limit is per form");
        received.Count.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(ShippedPermitLimit + 1);
        received.Take(ShippedPermitLimit).Should().OnlyContain(c => c.Honeypot == "http://bot.example");
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, Guid formId, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/forms/{formId}/submissions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        return client.SendAsync(request);
    }
}
