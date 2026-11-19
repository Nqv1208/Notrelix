using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The list of deleted Teams over HTTP on the application login (UC-WSP-L09): the Owner reads the Workspace's deleted
/// Teams, a Member is refused, and a person of another tenant who asks with tenant A's Workspace id gets the same
/// answer as for a made-up id and no Team name; the deleted Teams of tenant B never appear in tenant A's list.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DeletedTeamsAppRoleTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _member = null!;
    private WebApplicationFactory<Program>? _factory;

    public DeletedTeamsAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-deleted-teams@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-deleted-teams@example.com", "Bbb", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "TeamsMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        foreach (var (tenant, name) in new[] { (_a, "Aaa Retired team"), (_b, "Bbb Retired team") })
        {
            var team = Team.Create(tenant.AccountId, tenant.WorkspaceId, name, tenant.User.Id, now);
            team.Delete(tenant.User.Id, now);
            ctx.Teams.Add(team);
        }

        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body)> ReadAsync(User who, Guid workspaceId)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/teams/deleted");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    // The body may echo the requested id or a trace id; compare what the answer says, not which id it was asked about.
    private static string Normalize(string body) =>
        Regex.Replace(body, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|[0-9a-f]{32}|0H[0-9A-Z]{11}", "<id>");

    [Fact]
    public async Task TheOwner_ReadsOnlyTheirOwnWorkspacesDeletedTeams()
    {
        var read = await ReadAsync(_a.User, _a.WorkspaceId);

        read.Status.Should().Be(HttpStatusCode.OK);
        var names = JsonNode.Parse(read.Body)!.AsArray().Select(t => t!["name"]!.GetValue<string>()).ToArray();
        names.Should().Equal("Aaa Retired team");
        read.Body.Should().NotContain("Bbb");
    }

    [Fact]
    public async Task AMember_IsRefused_AndSeesNoTeamName()
    {
        var read = await ReadAsync(_member, _a.WorkspaceId);

        read.Status.Should().Be(HttpStatusCode.Forbidden);
        read.Body.Should().NotContain("Retired team");
    }

    [Fact]
    public async Task APersonOfAnotherTenant_GetsTheIdenticalNotFoundAsForAMadeUpWorkspace()
    {
        var found = await ReadAsync(_b.User, _a.WorkspaceId);
        var made = await ReadAsync(_b.User, Guid.NewGuid());

        made.Status.Should().Be(HttpStatusCode.NotFound);
        (found.Status, Normalize(found.Body)).Should().Be((made.Status, Normalize(made.Body)));
        found.Body.Should().NotContain("Aaa Retired team");
    }
}
