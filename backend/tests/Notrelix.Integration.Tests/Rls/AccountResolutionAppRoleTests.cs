using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Which Account a signed-in member acts in (<c>GET /api/v1/auth/bootstrap</c>), over the real HTTP graph under
/// the application role: the answer is the same on every call, follows the order default Workspace, owned
/// Account by name, and never names, or reveals the existence of, an Account of another tenant.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AccountResolutionAppRoleTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _zetaOfA;
    private Guid _alphaOfA;
    private Guid _aardvarkOfB;

    public AccountResolutionAppRoleTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        string hash;
        using (var f = CreateFactory(_fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword("Correct-Horse-9!");

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-resolution@example.com", "Aaa", hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-resolution@example.com", "Bbb", hash);

        // A owns two Accounts, "Zeta Co" written first and "Alpha Co" second, so insertion order points at the
        // wrong one. B owns one whose name sorts before both: it must never be chosen for A.
        _zetaOfA = await AddOwnedAccountAsync(_a, "Zeta Co");
        _alphaOfA = await AddOwnedAccountAsync(_a, "Alpha Co");
        _aardvarkOfB = await AddOwnedAccountAsync(_b, "Aardvark Co");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> CreateFactory(string db) => AppRoleHost.CreateFactory(_fixture, db);

    private async Task<Guid> AddOwnedAccountAsync(AppRoleTenant who, string name)
    {
        var now = DateTimeOffset.UtcNow;
        var account = Account.Create(name, $"{name.ToLowerInvariant().Replace(' ', '-')}-{Guid.NewGuid():N}"[..24],
            AccountType.Team, who.User.Id, now);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(tenant);
        ctx.Accounts.Add(account);
        ctx.AccountMembers.Add(AccountMember.Create(account.Id, who.User.Id, AccountRole.Owner, who.User.Id, now));
        ctx.AccessGrants.Add(new AccessGrant(
            accountId: account.Id, workspaceId: null, userId: who.User.Id, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: ["Owner"], permissionCodes: [], isAccountAdmin: true,
            isWorkspaceAdmin: false, grantedAt: now));
        await ctx.SaveChangesAsync();
        return account.Id;
    }

    private async Task<HttpResponseMessage> AsAsync(AppRoleTenant who, Func<HttpClient, Task<HttpResponseMessage>> call)
    {
        using var factory = CreateFactory(_fixture.AppConnectionString);
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who.User);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        var response = await call(client);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<(Guid? AccountId, string Body)> BootstrapAsync(AppRoleTenant who)
    {
        var response = await AsAsync(who, c => c.GetAsync("/api/v1/auth/bootstrap"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var account = JsonDocument.Parse(body).RootElement.GetProperty("accountId");
        return (account.ValueKind == JsonValueKind.Null ? null : account.GetGuid(), body);
    }

    [Fact]
    public async Task WithoutADefaultWorkspace_TheOwnedAccountWithTheFirstNameIsChosen_OnEveryCall()
    {
        var answers = new List<Guid?>();
        for (var call = 0; call < 4; call++)
            answers.Add((await BootstrapAsync(_a)).AccountId);

        answers.Should().OnlyContain(id => id == _alphaOfA, "owned Accounts are ordered by name: Alpha Co before Zeta Co");
    }

    [Fact]
    public async Task TheDefaultWorkspacesAccount_WinsOverTheOwnedAccounts()
    {
        var put = await AsAsync(_a, c => c.PutAsJsonAsync("/api/v1/account/default-workspace", new { workspaceId = _a.WorkspaceId }));
        put.StatusCode.Should().Be(HttpStatusCode.NoContent, await put.Content.ReadAsStringAsync());

        (await BootstrapAsync(_a)).AccountId.Should().Be(_a.AccountId);
    }

    [Fact]
    public async Task AnAccountScopedRoute_WorksForAMember_AndAnswersANonMembersAccountLikeAnUnknownId()
    {
        (await AsAsync(_a, c => c.GetAsync($"/api/v1/accounts/{_alphaOfA}/workspaces"))).StatusCode
            .Should().Be(HttpStatusCode.OK, "a member names an Account they belong to");

        var ofAnotherTenant = await AsAsync(_a, c => c.GetAsync($"/api/v1/accounts/{_aardvarkOfB}/workspaces"));
        var unknown = await AsAsync(_a, c => c.GetAsync($"/api/v1/accounts/{Guid.NewGuid()}/workspaces"));

        // An Account-scoped request keeps Forbidden for a non-member (the policy engine's rule: the Account comes
        // from the caller's own session); what matters is that an existing Account is answered exactly as an unknown one.
        ofAnotherTenant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        unknown.StatusCode.Should().Be(ofAnotherTenant.StatusCode);
        // The body echoes the caller's own request path (instance) and per-request trace and correlation ids; every other
        // field must be the same, so an Account that exists is not told apart from one that does not.
        var existingBody = await WithoutPerRequestFieldsAsync(ofAnotherTenant);
        var unknownBody = await WithoutPerRequestFieldsAsync(unknown);
        existingBody.Should().NotBeEmpty();
        existingBody.Should().BeEquivalentTo(unknownBody);
    }

    private static async Task<Dictionary<string, string>> WithoutPerRequestFieldsAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.EnumerateObject()
            .Where(p => p.Name is not ("instance" or "traceId" or "correlationId"))
            .ToDictionary(p => p.Name, p => p.Value.GetRawText());
    }

    [Fact]
    public async Task AnotherTenantsAccount_IsNeverChosenOrRevealed()
    {
        var (accountOfB, bodyOfB) = await BootstrapAsync(_b);
        accountOfB.Should().Be(_aardvarkOfB);
        bodyOfB.Should().NotContain(_alphaOfA.ToString()).And.NotContain(_zetaOfA.ToString());

        var (accountOfA, bodyOfA) = await BootstrapAsync(_a);
        accountOfA.Should().NotBe(_aardvarkOfB, "B's Account sorts first by name but is not A's");
        bodyOfA.Should().NotContain(_aardvarkOfB.ToString());
    }
}
