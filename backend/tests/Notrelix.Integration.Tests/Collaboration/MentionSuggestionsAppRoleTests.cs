using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Collaboration;

/// <summary>
/// UC-COL-B05 / UC-DCT-E08 over HTTP on the application login, through the production DI graph: the @ suggestions of a
/// page list only active colleagues of its workspace who can view it. A member denied the page, a guest and a person
/// of another account never appear, and a person who cannot open the page is refused before any name is read.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class MentionSuggestionsAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _viewer = null!;
    private User _blocked = null!;
    private Guid _page;
    private WebApplicationFactory<Program>? _factory;

    public MentionSuggestionsAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-mention-sugg@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-mention-sugg@example.com", "Miaotherworld", "hash");
        _viewer = await AppRoleMembers.AddAsync(_fixture, _a, "MiaViewer", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _blocked = await AppRoleMembers.AddAsync(_fixture, _a, "MiaBlocked", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        await AppRoleMembers.AddAsync(_fixture, _a, "MiaGuest", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");
        await AppRoleMembers.AddAsync(_fixture, _a, "MiaSuspended", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash");

        var owner = _a.User.Id;
        var page = Page.Create(_a.AccountId, _a.WorkspaceId, "Roadmap notes", owner, Now);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Pages.Add(page);
        ctx.PermissionRules.Add(PermissionRule.Create(
            _a.AccountId, _a.WorkspaceId, PermissionScopeType.Board, ResourceKind.Create("documents.page"), page.Id,
            PermissionSubjectType.User, _blocked.Id, null, PermissionAction.ViewPage, PermissionEffect.Deny, owner, Now));
        await ctx.SaveChangesAsync();
        _page = page.Id;
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, string Body)> SuggestAsync(User who, string query)
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync($"/api/v1/pages/{_page}/mention-suggestions?query={Uri.EscapeDataString(query)}&limit=20");
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ThePageOwner_IsSuggestedOnlyTheColleagueWhoCanViewThePage()
    {
        var (status, body) = await SuggestAsync(_a.User, "mia");

        status.Should().Be(HttpStatusCode.OK);
        var rows = JsonNode.Parse(body)!.AsArray();
        rows.Select(r => r!["userId"]!.GetValue<Guid>()).Should().Equal(_viewer.Id);
        rows.Single()!.AsObject().Select(p => p.Key).Should().BeEquivalentTo("userId", "name", "avatarUrl");
        body.Should().NotContain("Blocked").And.NotContain("Guest").And.NotContain("Suspended").And.NotContain("Miaotherworld");
        body.Should().NotContain("@example.com", "no email is returned");
    }

    [Fact]
    public async Task AMemberDeniedThePage_IsRefusedAndLearnsNoName()
    {
        var (status, body) = await SuggestAsync(_blocked, "mia");

        new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound }.Should().Contain(status);
        body.Should().NotContain("MiaViewer");
    }

    [Fact]
    public async Task APersonOfAnotherAccount_IsRefusedAndLearnsNoName()
    {
        var (status, body) = await SuggestAsync(_b.User, "mia");

        new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound }.Should().Contain(status);
        body.Should().NotContain("MiaViewer");
    }
}
