using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The tenant billing reads of a Workspace answer the same way for a person of another account and for a person of
/// the same account who holds no seat in that Workspace, whether the Workspace exists or not (UC-BIL-X01, NRX-003,
/// NRX-004). Proven over the real production graph on the application role, the same way as the Workspace reads in
/// <see cref="NonMemberDisclosureTests"/>. The refusal is 403 or 404; the test asserts the two answers are equal
/// and that the status is one of those two, without fixing which.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class BillingNonMemberDisclosureTests : IAsyncLifetime
{
    private static readonly string[] BillingReads = ["billing/plan", "billing/capabilities"];

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _sameAccountOutsider = null!;
    private User _accountAdmin = null!;

    public BillingNonMemberDisclosureTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-billing-x01@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-billing-x01@example.com", "Bbb", "hash");
        _sameAccountOutsider = await AppRoleUsers.AddAsync(_fixture, "outsider-billing-x01@example.com", "Out", "hash");
        _accountAdmin = await AppRoleUsers.AddAsync(_fixture, "admin-billing-x01@example.com", "Adm", "hash");

        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _sameAccountOutsider.Id, AccountRole.Member, _a.User.Id, now));
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _accountAdmin.Id, AccountRole.Admin, _a.User.Id, now));
        ctx.AccessGrants.Add(new Notrelix.Infrastructure.Data.Authz.AccessGrant(
            accountId: _a.AccountId, workspaceId: null, userId: _accountAdmin.Id, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: ["Admin"], permissionCodes: [], isAccountAdmin: true,
            isWorkspaceAdmin: false, grantedAt: now));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program>? _factory;

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body)> GetAsync(User who, string url)
    {
        string token;
        using (var scope = Factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync(url);
        return (response.StatusCode, Normalize(await response.Content.ReadAsStringAsync()));
    }

    // The body may echo the requested id or a trace id; compare what the answer says, not which id it was asked about.
    private static string Normalize(string body) =>
        Regex.Replace(body, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}", "<id>");

    private async Task AssertBillingReadsIdenticalAsync(User who, string because)
    {
        foreach (var route in BillingReads)
        {
            var found = await GetAsync(who, $"/api/v1/workspaces/{_a.WorkspaceId}/{route}");
            var made = await GetAsync(who, $"/api/v1/workspaces/{Guid.NewGuid()}/{route}");
            made.Status.Should().BeOneOf(new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound }, $"{route}: a made-up workspace is refused");
            found.Status.Should().Be(made.Status, $"{route}: {because}");
            found.Body.Should().Be(made.Body, $"{route}: {because}");
        }
    }

    [Fact]
    public async Task MemberOfAnotherAccount_GetsTheIdenticalRefusal_ForExistingAndMadeUpWorkspaces()
    {
        await AssertBillingReadsIdenticalAsync(_b.User, "a person of another account learns nothing about the billing of this workspace");
    }

    [Fact]
    public async Task SameAccountOutsider_GetsTheIdenticalRefusal_ForExistingAndMadeUpWorkspaces()
    {
        await AssertBillingReadsIdenticalAsync(_sameAccountOutsider, "a person of the same account without a seat in the workspace learns nothing");
    }

    [Fact]
    public async Task AccountAdminWithoutWorkspaceSeat_GetsTheIdenticalRefusal_ForExistingAndMadeUpWorkspaces()
    {
        await AssertBillingReadsIdenticalAsync(_accountAdmin, "an account admin without a seat in the workspace learns nothing");
    }
}
