using System.Net;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.ResolveSlug;

namespace Notrelix.API.Tests.RateLimiting;

/// <summary>
/// UC-WSP-B02: the anonymous resolve-by-address route is limited per IP under the shipped
/// <c>GeneralAnonymous</c> tier (60 per minute), so addresses cannot be enumerated at speed. The limit is
/// answered before the handler runs.
/// </summary>
public sealed class ResolveSlugRateLimitTests
{
    private const int ShippedPermitLimit = 60;

    [Fact]
    public async Task Resolve_SixtyFirstLookupFromOneAddress_IsRefusedWith429_BeforeTheHandler()
    {
        var handlerCalls = 0;
        await using var factory = new ShippedRateLimitsApiFactory().WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRequestHandler<ResolveSlugQuery, Result<WorkspaceDto>>>();
            services.AddScoped<IRequestHandler<ResolveSlugQuery, Result<WorkspaceDto>>>(_ =>
            {
                var handler = new Mock<IRequestHandler<ResolveSlugQuery, Result<WorkspaceDto>>>();
                handler.Setup(h => h.Handle(It.IsAny<ResolveSlugQuery>(), It.IsAny<CancellationToken>()))
                    .Callback(() => handlerCalls++)
                    .ReturnsAsync(Result<WorkspaceDto>.Failure("Workspace not found."));
                return handler.Object;
            });
        }));
        var client = factory.CreateClient();
        var path = $"/api/v1/accounts/{Guid.NewGuid()}/resolve?slug=probe";

        for (var attempt = 1; attempt <= ShippedPermitLimit; attempt++)
        {
            var response = await client.GetAsync(path);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"lookup {attempt} is within the shipped limit");
        }

        var limited = await client.GetAsync(path);

        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.Should().NotBeNull();
        limited.Headers.RetryAfter!.Delta!.Value.TotalSeconds.Should().BeGreaterThan(0);
        limited.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var body = JsonDocument.Parse(await limited.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errorCode").GetString().Should().Be("rate_limit.exceeded");
        handlerCalls.Should().Be(ShippedPermitLimit, "the limited lookup never reaches the resolver");
    }
}
