using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;

namespace Notrelix.API.Tests.Contracts;

/// <summary>
/// M7 HTTP ingress evidence — the Documents/Collaboration write contracts go
/// through the real endpoint pipeline: route matching, JSON body binding into
/// the command (including the optional mentionedUserIds array), the real
/// MediatR pipeline, real handlers, and response mapping. No capturing fake
/// replaces the application path; persistence is inspected through the
/// In-Memory application context the factory hosts.
/// </summary>
public class M7IngressEndpointTests : IClassFixture<NotrelixApiFactory>
{
    private const string TestAccountId = "A0000000-0000-0000-0000-000000000001";
    private const string TestWorkspaceId = "A0000000-0000-0000-0000-000000000001";

    private readonly WebApplicationFactory<Program> _factory;

    public M7IngressEndpointTests(NotrelixApiFactory factory)
    {
        _factory = factory;
    }

    private static string PageRoute(Guid pageId) => $"/api/v1/pages/{pageId}/archive";

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private static Guid TestUserId() => Guid.Parse(TestAuthHandler.TestUserId);

    [Fact]
    public async Task UnauthenticatedArchivePage_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(PageRoute(Guid.NewGuid()), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnauthenticatedCreateComment_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/v1/board-items/{Guid.NewGuid()}/comments",
            JsonBody(new { contentMd = "hello" }));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ArchivePage_AuthorizedActor_Returns204_AndRunsRealHandler()
    {
        // The factory's always-allow facts mirror an owner; the resource scope
        // is stubbed to the seeded tenant (the In-Memory host carries no
        // production locator rows), while the real handler loads the seeded
        // page from the In-Memory context. The aggregate assigns its own Id,
        // so the test resolves the route from the persisted entity.
        var pageId = await SeedPageAsync();

        var factory = WithResourceScope();
        using var client = factory.CreateClientWithAuth();

        var response = await client.PostAsync(PageRoute(pageId), null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "the archive endpoint maps the succeeded Result to 204 No Content");

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await context.Pages.SingleAsync(p => p.Id == pageId)).Status.Should().Be(PageStatus.Archived,
            "the real Page.Archive authority must have executed through the HTTP path");
    }

    [Fact]
    public async Task ArchivePage_MissingPage_Returns404_AndRunsRealHandler()
    {
        // A real locator resolves to null for an unknown resource — the
        // canonical pipeline fails closed NotFound before any mutation.
        using var client = _factory.CreateClientWithAuth();

        var response = await client.PostAsync(PageRoute(Guid.NewGuid()), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the canonical pipeline fails closed NotFound before any lifecycle mutation");
    }

    [Fact]
    public async Task CreateComment_WithMentionedUserIds_BindsBodyThroughRealPipeline()
    {
        var mentionedUser1 = Guid.NewGuid();
        var mentionedUser2 = Guid.NewGuid();
        var boardItemId = Guid.NewGuid();
        var content = $"ingress content {Guid.NewGuid():N}";

        var factory = WithResourceScope();
        using var client = factory.CreateClientWithAuth();
        var response = await client.PostAsync(
            $"/api/v1/board-items/{boardItemId}/comments",
            JsonBody(new { contentMd = content, mentionedUserIds = new[] { mentionedUser1, mentionedUser2 } }));

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "the create-comment endpoint maps the succeeded Result<Guid> to 201 Created");

        // Real handler executed: the comment and both mention entities exist.
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored = await context.Comments
            .Where(c => c.Target.ResourceId == boardItemId)
            .SingleAsync();
        stored.Content.Should().Be(content,
            "the JSON body must bind ContentMd through the endpoint into the command");
        stored.Target.Kind.Value.Should().Be("work-management.board-item");
        stored.CreatedBy.Should().Be(TestUserId());

        var mentions = await context.PageMentions
            .ToListAsync();
        mentions.Select(m => m.MentionedId)
            .Should().BeEquivalentTo([mentionedUser1, mentionedUser2],
            "the optional mentionedUserIds array must travel the HTTP boundary into the real handler");
        mentions.Select(m => m.MentionedByUserId)
            .Should().OnlyContain(id => id == TestUserId(),
            "the handler captures the authenticated actor as the trusted mentioner");
    }

    private async Task<Guid> SeedPageAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var page = Page.Create(
            Guid.Parse(TestAccountId),
            Guid.Parse(TestWorkspaceId),
            "Ingress Page",
            TestUserId(),
            DateTimeOffset.UtcNow);
        context.Pages.Add(page);
        await context.SaveChangesAsync();
        return page.Id;
    }

    /// <summary>
    /// The In-Memory host carries no production locator rows, so resource
    /// resolution is stubbed to the factory's seeded tenant — the same seam
    /// the idempotency contract tests stub. Everything after the locator
    /// (pipeline, binding, real handlers) stays production code.
    /// </summary>
    private WebApplicationFactory<Program> WithResourceScope()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IResourceLocator>();
                services.AddScoped<IResourceLocator>(_ =>
                {
                    var mock = new Moq.Mock<IResourceLocator>();
                    mock.Setup(x => x.LocateAsync(
                            Moq.It.IsAny<ResourceRef>(), Moq.It.IsAny<Guid>(), Moq.It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new ResourceLocation(
                            ResourceKind.Create("documents.page"),
                            Guid.NewGuid(),
                            Guid.Parse(TestAccountId),
                            Guid.Parse(TestWorkspaceId)));
                    return mock.Object;
                });
            });
        });
    }
}

internal static class M7IngressTestClientExtensions
{
    public static HttpClient CreateClientWithAuth(this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        return client;
    }
}
