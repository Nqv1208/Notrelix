using System.Net;
using System.Text.Json.Nodes;
using JsonValue = Notrelix.Domain.SharedKernel.JsonValue;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Documents.Blocks;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Versions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// UC-DCT-I01: every Documents endpoint that takes a page, block, version, template or workspace id answers a person
/// outside the Workspace with the same status and the same body whether the id belongs to another tenant or was
/// never issued. Proven per route over the production graph on the application role, for a person of another
/// account and a person of the same account who is not in the Workspace. The status is not hard-coded: a refusal
/// is either Forbidden or NotFound, and the two answers must match each other. Request-specific fields
/// (traceId, correlationId, instance) and ids are removed before the bodies are compared.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DocumentsNonMemberNotFoundTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly HttpStatusCode[] Refusals = [HttpStatusCode.Forbidden, HttpStatusCode.NotFound];
    private const string GuidPattern = "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}";
    private const string Text = "{\"v\":1,\"runs\":[{\"text\":\"hi\",\"marks\":[]}]}";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _sameAccountOutsider = null!;
    private Guid _pageId;
    private Guid _blockId;
    private Guid _versionId;
    private Guid _templateId;
    private WebApplicationFactory<Program>? _factory;

    public DocumentsNonMemberNotFoundTests(AppRoleTestContainer fixture) => _fixture = fixture;

    /// <summary>The ids a route is built from: the real ones of tenant A, or made-up ones.</summary>
    private sealed record Ids(Guid Workspace, Guid Page, Guid Block, Guid Version, Guid Template);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-docs-i01@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-docs-i01@example.com", "Bbb", "hash");
        _sameAccountOutsider = await AppRoleUsers.AddAsync(_fixture, "outsider-docs-i01@example.com", "Out", "hash");

        var owner = _a.User.Id;
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Secret plan", owner, Now);
        var block = Block.CreateRoot(_a.AccountId, _a.WorkspaceId, page.Id, BlockType.Text,
            BlockContent.Create(JsonValue.Create(Text)), FractionalIndex.Create("a0"), owner, Now);
        var version = DocumentVersion.Create(_a.AccountId, _a.WorkspaceId, page.Id, 1,
            DocumentSnapshot.Create(JsonValue.Create("{\"title\":\"Secret plan\",\"blocks\":[]}")), owner, Now, "Before launch", DocumentVersionKind.Named);
        var template = PageTemplate.Create("Plan", JsonValue.Create("{\"title\":\"Plan\"}"), JsonValue.Create("[]"), Now, _a.WorkspaceId, owner);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Pages.Add(page);
        ctx.Blocks.Add(block);
        ctx.DocumentVersions.Add(version);
        ctx.PageTemplates.Add(template);
        await ctx.SaveChangesAsync();

        _pageId = page.Id;
        _blockId = block.Id;
        _versionId = version.Id;
        _templateId = template.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private Ids Existing() => new(_a.WorkspaceId, _pageId, _blockId, _versionId, _templateId);

    private static Ids MadeUp() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    /// <summary>Every read or write a person outside the Workspace can send with a Documents id.</summary>
    private static IEnumerable<(string Name, HttpMethod Method, Func<Ids, string> Url, Func<Ids, string>? Json)> Routes()
    {
        yield return ("list pages", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/pages", null);
        yield return ("page tree", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/pages/tree", null);
        yield return ("search pages", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/pages/search?query=secret", null);
        yield return ("list the trash", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/pages/trash", null);
        yield return ("create a page", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/pages", i => "{\"title\":\"x\"}");
        yield return ("read a page", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}", null);
        yield return ("rename a page", HttpMethod.Patch, i => $"/api/v1/pages/{i.Page}", i => "{\"title\":\"x\",\"expectedVersion\":1}");
        yield return ("trash a page", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/archive", null);
        yield return ("restore a page", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/restore", null);
        yield return ("delete a page", HttpMethod.Delete, i => $"/api/v1/pages/{i.Page}", null);
        yield return ("duplicate a page", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/pages/{i.Page}/duplicate", i => "{}");
        yield return ("breadcrumb", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/breadcrumb", null);
        yield return ("page history", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/history", null);
        yield return ("backlinks", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/backlinks", null);
        yield return ("move a page", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/move", i => "{\"parentId\":null,\"expectedVersion\":1}");
        yield return ("move a page to a space", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/move-to-space", i => "{\"spaceId\":null,\"expectedVersion\":1}");
        yield return ("list blocks", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/blocks", null);
        yield return ("create a block", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/blocks", i => "{\"type\":\"paragraph\"}");
        yield return ("save blocks in a batch", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/blocks/batch",
            i => $"{{\"blocks\":[{{\"id\":\"{Guid.NewGuid()}\",\"type\":\"paragraph\"}}]}}");
        yield return ("edit a block", HttpMethod.Patch, i => $"/api/v1/pages/{i.Page}/blocks/{i.Block}", i => "{\"expectedVersion\":1}");
        yield return ("delete a block", HttpMethod.Delete, i => $"/api/v1/pages/{i.Page}/blocks/{i.Block}?expectedVersion=1", null);
        yield return ("reorder blocks", HttpMethod.Post, i => "/api/v1/blocks/reorder",
            i => $"{{\"pageId\":\"{i.Page}\",\"items\":[{{\"blockId\":\"{i.Block}\",\"expectedVersion\":1}}]}}");
        yield return ("list versions", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/versions", null);
        yield return ("read a version", HttpMethod.Get, i => $"/api/v1/pages/{i.Page}/versions/{i.Version}", null);
        yield return ("save a named version", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/versions", i => "{\"name\":\"x\"}");
        yield return ("restore a version", HttpMethod.Post, i => $"/api/v1/pages/{i.Page}/versions/{i.Version}/restore", i => "{\"currentBlocks\":[]}");
        yield return ("list templates", HttpMethod.Get, i => $"/api/v1/workspaces/{i.Workspace}/page-templates", null);
        yield return ("save a template", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/page-templates", i => $"{{\"pageId\":\"{i.Page}\",\"name\":\"x\"}}");
        yield return ("create a page from a template", HttpMethod.Post, i => $"/api/v1/workspaces/{i.Workspace}/page-templates/{i.Template}/pages", i => "{}");
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(User who, HttpMethod method, string url, string? json)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var request = new HttpRequestMessage(method, url);
        // Every write carries a fresh key, so a replayed answer can never stand in for the one being compared.
        if (method != HttpMethod.Get) request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
        if (json is not null) request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    // Drop the per-request fields of a problem body, then the ids it may echo.
    private static string Normalize(string body)
    {
        var text = body;
        try
        {
            if (JsonNode.Parse(body) is JsonObject obj)
            {
                var noise = obj.Select(kv => kv.Key)
                    .Where(key => key is "traceId" or "correlationId" or "instance" or "requestId")
                    .ToList();
                foreach (var key in noise) obj.Remove(key);
                text = obj.ToJsonString();
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Not JSON (an empty body): compare the text as it is.
        }
        return Regex.Replace(text, GuidPattern, "<id>");
    }

    private async Task AssertSameRefusalAsync(User who, string because)
    {
        foreach (var (name, method, url, json) in Routes())
        {
            var real = Existing();
            var fake = MadeUp();
            var found = await SendAsync(who, method, url(real), json?.Invoke(real));
            var made = await SendAsync(who, method, url(fake), json?.Invoke(fake));
            Refusals.Should().Contain(made.Status, $"{name}: a made-up id is refused as Forbidden or NotFound");
            found.Status.Should().Be(made.Status, $"{name}: {because}");
            found.Body.Should().Be(made.Body, $"{name}: {because}");
        }
    }

    [Fact]
    public async Task MemberOfAnotherAccount_GetsTheSameRefusal_ForAnotherTenantAndMadeUpIds() =>
        await AssertSameRefusalAsync(_b.User, "a person of another account learns nothing about another tenant's page, block, version or template");

    [Fact]
    public async Task SameAccountOutsider_GetsTheSameRefusal_ForAnotherTenantAndMadeUpIds() =>
        await AssertSameRefusalAsync(_sameAccountOutsider, "a person of the same account outside the Workspace learns nothing about its pages, blocks, versions or templates");

    [Fact]
    public async Task EveryDocumentsRoute_IsCoveredByThisTest()
    {
        // A new Documents route must get a row above, or this fails.
        var routes = Factory.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.IRouteNameMetadata>()?.RouteName?.StartsWith("Documents.", StringComparison.Ordinal) == true)
            .Select(e => e.RoutePattern.RawText)
            .Distinct()
            .Count();
        Routes().Count().Should().BeGreaterThanOrEqualTo(routes, "each Documents route has a row in Routes()");
    }
}
