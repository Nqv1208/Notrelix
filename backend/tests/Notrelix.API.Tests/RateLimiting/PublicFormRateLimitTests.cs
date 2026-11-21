using System.Net;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Forms.Commands.SubmitPublicForm;
using Notrelix.Application.Features.WorkManagement.Forms.Queries.GetPublicForm;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// Anti-abuse of the anonymous form routes under the shipped tiers: a visitor's address is limited per form
/// (5 submissions a minute), the form is also capped as a whole whoever calls, and a refused request never
/// reaches the handler. The routes are reachable with no sign-in at all.
/// </summary>
public sealed class PublicFormRateLimitTests
{
    private const int ShippedSubmitPerAddress = 5;
    private const string KeyA = "form-key-aaaaaaaaaaaaaaaa";
    private const string KeyB = "form-key-bbbbbbbbbbbbbbbb";

    private static WebApplicationFactoryHandle Host(List<SubmitPublicFormCommand> received, Action<IConfigurationBuilder>? configure = null)
    {
        var factory = new ShippedRateLimitsApiFactory().WithWebHostBuilder(builder =>
        {
            if (configure is not null) builder.ConfigureAppConfiguration((_, config) => configure(config));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRequestHandler<SubmitPublicFormCommand, Result>>();
                services.AddScoped<IRequestHandler<SubmitPublicFormCommand, Result>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<SubmitPublicFormCommand, Result>>();
                    handler.Setup(h => h.Handle(It.IsAny<SubmitPublicFormCommand>(), It.IsAny<CancellationToken>()))
                        .Callback<SubmitPublicFormCommand, CancellationToken>((c, _) => received.Add(c))
                        .ReturnsAsync(Result.Success());
                    return handler.Object;
                });
                services.RemoveAll<IRequestHandler<GetPublicFormQuery, Result<PublicFormDto>>>();
                services.AddScoped<IRequestHandler<GetPublicFormQuery, Result<PublicFormDto>>>(_ =>
                {
                    var handler = new Mock<IRequestHandler<GetPublicFormQuery, Result<PublicFormDto>>>();
                    handler.Setup(h => h.Handle(It.IsAny<GetPublicFormQuery>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<PublicFormDto>.Success(new PublicFormDto("F", null, [], "site")));
                    return handler.Object;
                });
            });
        });
        return new WebApplicationFactoryHandle(factory);
    }

    private sealed class WebApplicationFactoryHandle(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory) : IAsyncDisposable
    {
        public HttpClient Client { get; } = factory.CreateClient();
        public ValueTask DisposeAsync() => factory.DisposeAsync();
    }

    private static Task<HttpResponseMessage> Post(HttpClient client, string key) =>
        client.PostAsync($"/api/v1/public/forms/{key}/submissions",
            new StringContent("""{"answers":{},"captchaToken":"t"}""", Encoding.UTF8, "application/json"));

    [Fact]
    public async Task Submit_NeedsNoSignIn_AndReachesTheHandlerWithTheKeyAndToken()
    {
        var received = new List<SubmitPublicFormCommand>();
        await using var host = Host(received);

        var response = await Post(host.Client, KeyA);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        received.Should().ContainSingle().Which.Should().Match<SubmitPublicFormCommand>(c => c.PublicKey == KeyA && c.CaptchaToken == "t");
    }

    [Fact]
    public async Task Submit_SixthFromOneAddressToOneForm_Is429WithVietnameseMessage_AndOtherFormStillPasses()
    {
        var received = new List<SubmitPublicFormCommand>();
        await using var host = Host(received);

        for (var attempt = 1; attempt <= ShippedSubmitPerAddress; attempt++)
            (await Post(host.Client, KeyA)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"submission {attempt} is within the limit");

        var limited = await Post(host.Client, KeyA);
        var other = await Post(host.Client, KeyB);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Be("Bạn đã gửi quá nhiều phản hồi cho biểu mẫu này. Vui lòng thử lại sau ít phút.");
        other.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, "the limit is per form");
        received.Should().HaveCount(ShippedSubmitPerAddress + 1, "the limited submission never reached the handler");
    }

    [Fact]
    public async Task Form_IsCappedAsAWhole_WhoeverCalls_BeforeTheAddressLimit()
    {
        var received = new List<SubmitPublicFormCommand>();
        // Lower only the per-form cap; the per-address cap stays the shipped 5, so a trip at 3 is the form's own.
        await using var host = Host(received, config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:Policies:PublicFormByForm:PermitLimit"] = "3",
        }));

        for (var attempt = 1; attempt <= 3; attempt++)
            (await Post(host.Client, KeyA)).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        var limited = await Post(host.Client, KeyA);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detail").GetString().Should().Be("Biểu mẫu này đang nhận quá nhiều yêu cầu. Vui lòng thử lại sau ít phút.");
        received.Should().HaveCount(3);
    }

    [Fact]
    public async Task Read_NeedsNoSignIn_AndIsLimitedPerAddressAndForm()
    {
        await using var host = Host([]);

        var ok = await host.Client.GetAsync($"/api/v1/public/forms/{KeyA}");
        ok.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var attempt = 2; attempt <= 60; attempt++)
            (await host.Client.GetAsync($"/api/v1/public/forms/{KeyA}")).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

        (await host.Client.GetAsync($"/api/v1/public/forms/{KeyA}")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TheAdminSwitch_NeedsASignIn()
    {
        await using var host = Host([]);

        var put = await host.Client.PutAsync($"/api/v1/forms/{Guid.NewGuid()}/public-access",
            new StringContent("""{"enabled":true}""", Encoding.UTF8, "application/json"));
        var get = await host.Client.GetAsync($"/api/v1/forms/{Guid.NewGuid()}/public-access");

        put.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        get.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
