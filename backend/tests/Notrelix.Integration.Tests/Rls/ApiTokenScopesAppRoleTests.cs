using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// A token limited to chosen scopes, over the production graph on the application login (dependency injection as in
/// production, an app login that is not the owner, row-level security on): a call outside the token's scopes is
/// answered 403 identity.token.scope-missing before any handler runs; a scope only narrows (a token of every scope
/// still cannot exceed its issuer, nor reach a request no scope covers); an Owner picks the scopes at issue, a token
/// has at least one, and a Member cannot issue one.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ApiTokenScopesAppRoleTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private const string ScopeMissing = "identity.token.scope-missing";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private User _member = null!;
    private WebApplicationFactory<Program>? _factory;

    public ApiTokenScopesAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var scope = Factory.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "owner-scopes@example.com", "Owner", hash);
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "Member", WorkspaceRole.Member, WorkspaceMemberStatus.Active, hash);
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> IssueAsync(User issuer, params string[] scopes)
    {
        var secret = new ApiTokenSecretService().Generate();
        var token = ApiToken.Create(
            _a.AccountId, _a.WorkspaceId, issuer.Id, $"scoped-{Guid.NewGuid():N}"[..16], secret.TokenHash,
            ApiTokenScopes.Create(scopes), issuer.Id, DateTimeOffset.UtcNow, expiresAt: null);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ApiTokens.Add(token);
        await ctx.SaveChangesAsync();
        return secret.RawToken;
    }

    private HttpClient Bearer(string rawToken)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", rawToken);
        return client;
    }

    private async Task<HttpClient> SignInAsync(string email)
    {
        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        return client;
    }

    private string WorkspaceRoute(string suffix) => $"/api/v1/workspaces/{_a.WorkspaceId}/{suffix}";

    private static HttpRequestMessage Post(string url, string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        return request;
    }

    private static async Task AssertScopeMissingAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().Contain(ScopeMissing);
    }

    [Fact]
    public async Task AReadOnlyToken_ReadsButIsRefusedOnAWrite_With403AndTheScopeCode()
    {
        using var client = Bearer(await IssueAsync(_a.User, "work:read"));

        (await client.GetAsync(WorkspaceRoute("boards"))).StatusCode.Should().Be(HttpStatusCode.OK, "work:read covers reading boards");

        await AssertScopeMissingAsync(await client.SendAsync(Post(WorkspaceRoute("boards"), """{"title":"Written by a read-only token"}""")));

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var check = _fixture.CreateOwnerContext(system);
        (await check.Boards.AnyAsync(b => b.Title == "Written by a read-only token")).Should().BeFalse("the refused write stored nothing");
    }

    [Fact]
    public async Task AWriteToken_DoesNotImplyRead_AndAWriteScopeCoversTheWrite()
    {
        using var client = Bearer(await IssueAsync(_a.User, "work:write"));

        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("boards")));

        var created = await client.SendAsync(Post(WorkspaceRoute("boards"), """{"title":"Written by a write token"}"""));
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AWorkToken_IsRefusedOnADocsRoute_AndADocsTokenIsNot()
    {
        using var workToken = Bearer(await IssueAsync(_a.User, "work:read", "work:write"));
        await AssertScopeMissingAsync(await workToken.GetAsync(WorkspaceRoute("pages")));

        using var docsToken = Bearer(await IssueAsync(_a.User, "docs:read"));
        (await docsToken.GetAsync(WorkspaceRoute("pages"))).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await docsToken.GetAsync(WorkspaceRoute("boards")));
    }

    [Fact]
    public async Task AResourceRoute_NeedsTheScopeOfItsContext()
    {
        using var docsToken = Bearer(await IssueAsync(_a.User, "docs:read", "docs:write"));
        await AssertScopeMissingAsync(await docsToken.GetAsync($"/api/v1/boards/{_a.BoardId}"));

        using var workToken = Bearer(await IssueAsync(_a.User, "work:read"));
        (await workToken.GetAsync($"/api/v1/boards/{_a.BoardId}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ATokenOfEveryScope_StillCannotExceedAMemberIssuer()
    {
        // ChatGroupManage is denied to a Member and allowed to an Owner; the token carries every scope in both cases.
        using var ofTheOwner = Bearer(await IssueAsync(_a.User, [.. ApiTokenScopes.Vocabulary]));
        using var ofTheMember = Bearer(await IssueAsync(_member, [.. ApiTokenScopes.Vocabulary]));

        var asOwner = await ofTheOwner.SendAsync(Post(WorkspaceRoute("chat/channel-groups"), """{"name":"Owner group"}"""));
        asOwner.StatusCode.Should().Be(HttpStatusCode.Created, await asOwner.Content.ReadAsStringAsync());

        var asMember = await ofTheMember.SendAsync(Post(WorkspaceRoute("chat/channel-groups"), """{"name":"Member group"}"""));
        var body = await asMember.Content.ReadAsStringAsync();
        asMember.StatusCode.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().NotContain(ScopeMissing, "the scope is there; the refusal is the issuer's own permission");
    }

    [Fact]
    public async Task ATokenOfEveryScope_StillCannotReachARequestNoScopeCovers()
    {
        using var client = Bearer(await IssueAsync(_a.User, [.. ApiTokenScopes.Vocabulary]));

        // The token list (Identity), the member list (Workspaces beyond the read set) and a space (Workspaces write).
        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("api-tokens")));
        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("members")));
        await AssertScopeMissingAsync(await client.SendAsync(Post(WorkspaceRoute("spaces"), """{"name":"Made by a token"}""")));
    }

    [Fact]
    public async Task WorkspaceRead_CoversTheDefinedReadSetAndNothingMore()
    {
        using var client = Bearer(await IssueAsync(_a.User, "workspace:read"));

        (await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync(WorkspaceRoute("spaces"))).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("members")));
        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("boards")));
    }

    [Fact]
    public async Task ATokenWithNoScope_ReachesNothing()
    {
        // A token row with a null scope list (never migrated): an empty list means no access, never full access.
        var secret = new ApiTokenSecretService().Generate();
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.ApiTokens.Add(ApiToken.Create(
                _a.AccountId, _a.WorkspaceId, _a.User.Id, "no-scope", secret.TokenHash,
                scopes: null, createdBy: _a.User.Id, createdAt: DateTimeOffset.UtcNow, expiresAt: null));
            await ctx.SaveChangesAsync();
        }

        using var client = Bearer(secret.RawToken);

        await AssertScopeMissingAsync(await client.GetAsync($"/api/v1/workspaces/{_a.WorkspaceId}"));
        await AssertScopeMissingAsync(await client.GetAsync(WorkspaceRoute("boards")));
    }

    [Fact]
    public async Task IssuingATokenWithNoScope_IsRefused_AndOneWithScopesIsListedAndEnforced()
    {
        using var owner = await SignInAsync("owner-scopes@example.com");

        var none = await owner.PostAsJsonAsync(WorkspaceRoute("api-tokens"), new
        {
            name = "No scopes",
            expiresAt = DateTimeOffset.UtcNow.AddDays(30),
            scopes = Array.Empty<string>(),
            stepUpToken = "proof"
        });
        var noneBody = await none.Content.ReadAsStringAsync();
        none.StatusCode.Should().Be(HttpStatusCode.BadRequest, noneBody);
        noneBody.Should().Contain("An API token needs at least one scope.");

        var unknown = await owner.PostAsJsonAsync(WorkspaceRoute("api-tokens"), new
        {
            name = "Unknown scope",
            expiresAt = DateTimeOffset.UtcNow.AddDays(30),
            scopes = new[] { "admin" },
            stepUpToken = "proof"
        });
        var unknownBody = await unknown.Content.ReadAsStringAsync();
        unknown.StatusCode.Should().Be(HttpStatusCode.BadRequest, unknownBody);
        unknownBody.Should().Contain("Unknown API token scope: admin.");

        var stepUp = await owner.PostAsJsonAsync("/api/v1/auth/security/step-up/complete-password",
            new { purpose = "IssueApiToken", password = Password });
        stepUp.StatusCode.Should().Be(HttpStatusCode.OK, await stepUp.Content.ReadAsStringAsync());
        var proof = (await stepUp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("proofToken").GetString();

        var created = await owner.PostAsJsonAsync(WorkspaceRoute("api-tokens"), new
        {
            name = "Docs reader",
            expiresAt = DateTimeOffset.UtcNow.AddDays(30),
            scopes = new[] { "docs:read" },
            stepUpToken = proof
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        var secret = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rawSecret").GetString()!;

        var list = await owner.GetAsync(WorkspaceRoute("api-tokens"));
        var items = (await list.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items");
        items.EnumerateArray().Single(i => i.GetProperty("name").GetString() == "Docs reader")
            .GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).Should().Equal("docs:read");

        using var token = Bearer(secret);
        (await token.GetAsync(WorkspaceRoute("pages"))).StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertScopeMissingAsync(await token.GetAsync(WorkspaceRoute("boards")));
    }

    [Fact]
    public async Task AMember_CannotIssueAToken()
    {
        using var member = await SignInAsync(_member.Email.Value);

        var response = await member.PostAsJsonAsync(WorkspaceRoute("api-tokens"), new
        {
            name = "Member minted",
            expiresAt = DateTimeOffset.UtcNow.AddDays(30),
            scopes = new[] { "work:read" },
            stepUpToken = "proof"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, await response.Content.ReadAsStringAsync());
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var check = _fixture.CreateOwnerContext(system);
        (await check.ApiTokens.AnyAsync(t => t.Name == "Member minted")).Should().BeFalse();
    }
}
