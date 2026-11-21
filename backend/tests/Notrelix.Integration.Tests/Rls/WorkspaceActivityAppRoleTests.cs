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
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The workspace activity feed over HTTP on the application login, through the production container (so the handler and the
/// Collaboration port resolve as shipped): a member reads the facts of their workspace and not those of a Page they are denied,
/// the feed pages by cursor, a Guest is forbidden and a person of another tenant is told the workspace does not exist.
/// Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class WorkspaceActivityAppRoleTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _member = null!;
    private User _guest = null!;
    private WebApplicationFactory<Program>? _factory;

    public WorkspaceActivityAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-activity-http@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-activity-http@example.com", "Bbb", "hash");
        _member = await AppRoleMembers.AddAsync(_fixture, _a, "ActMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        _guest = await AppRoleMembers.AddAsync(_fixture, _a, "ActGuest", WorkspaceRole.Guest, WorkspaceMemberStatus.Active, "hash");

        var owner = _a.User.Id;
        var open = Page.Create(_a.AccountId, _a.WorkspaceId, "Open page", owner, Now);
        var denied = Page.Create(_a.AccountId, _a.WorkspaceId, "Denied page", owner, Now);

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.Pages.AddRange(open, denied);
        ctx.PermissionRules.Add(PermissionRule.Create(
            _a.AccountId, _a.WorkspaceId, PermissionScopeType.Board, ResourceKind.Create("documents.page"), denied.Id,
            PermissionSubjectType.User, _member.Id, null, PermissionAction.ViewPage, PermissionEffect.Deny, owner, Now));
        ctx.WorkspaceActivityLogs.AddRange(
            Fact(_a, "work.board-created", "Board", _a.BoardId, 4, null, null),
            PageFact(_a, open.Id, 3),
            PageFact(_a, denied.Id, 2),
            Fact(_b, "work.board-created", "Board", _b.BoardId, 1, null, null));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _factory?.Dispose();
        return Task.CompletedTask;
    }

    private static WorkspaceActivityLogRecord Fact(
        AppRoleTenant tenant, string type, string subjectType, Guid subjectId, int minutesAgo, string? resourceKind, Guid? resourceId) =>
        WorkspaceActivityLogRecord.Create(
            tenant.WorkspaceId, "work", type, subjectType, subjectId, Now.AddMinutes(-minutesAgo),
            sourceEventId: Guid.CreateVersion7(), actorUserId: tenant.User.Id, resourceType: resourceKind, resourceId: resourceId, title: type);

    private static WorkspaceActivityLogRecord PageFact(AppRoleTenant tenant, Guid pageId, int minutesAgo) =>
        WorkspaceActivityLogRecord.Create(
            tenant.WorkspaceId, "documents", "docs.page-renamed", "Page", pageId, Now.AddMinutes(-minutesAgo),
            sourceEventId: Guid.CreateVersion7(), actorUserId: tenant.User.Id, resourceType: "documents.page", resourceId: pageId, title: "renamed");

    private WebApplicationFactory<Program> Factory => _factory ??= AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);

    private async Task<(HttpStatusCode Status, JsonNode? Json)> GetAsync(User who, Guid workspaceId, string query = "")
    {
        var factory = Factory;
        string token;
        using (var scope = factory.Services.CreateScope())
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(who);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var response = await client.GetAsync($"/api/v1/workspaces/{workspaceId}/activity{query}");
        var body = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, response.IsSuccessStatusCode ? JsonNode.Parse(body) : null);
    }

    private static string[] Types(JsonNode json) =>
        json["items"]!.AsArray().Select(i => i!["type"]!.GetValue<string>()).ToArray();

    [Fact]
    public async Task AMember_ReadsTheWorkspaceFacts_ExceptThoseOfAPageTheyAreDenied()
    {
        var asMember = await GetAsync(_member, _a.WorkspaceId);
        asMember.Status.Should().Be(HttpStatusCode.OK);
        Types(asMember.Json!).Should().Equal(["docs.page-renamed", "work.board-created"], "newest first; the denied Page's fact and the other tenant's fact are absent");

        var asOwner = await GetAsync(_a.User, _a.WorkspaceId);
        Types(asOwner.Json!).Should().Equal(["docs.page-renamed", "docs.page-renamed", "work.board-created"]);
    }

    [Fact]
    public async Task TheFeed_PagesByCursor_AndAnOversizedLimitIsRefused()
    {
        var first = await GetAsync(_a.User, _a.WorkspaceId, "?limit=2");
        Types(first.Json!).Should().HaveCount(2);
        var cursor = first.Json!["nextCursor"]!.GetValue<string>();

        var second = await GetAsync(_a.User, _a.WorkspaceId, $"?limit=2&cursor={Uri.EscapeDataString(cursor)}");
        Types(second.Json!).Should().Equal("work.board-created");
        second.Json!["nextCursor"].Should().BeNull();

        (await GetAsync(_a.User, _a.WorkspaceId, "?limit=51")).Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AGuestIsForbidden_AndAnotherTenantIsNotFound()
    {
        (await GetAsync(_guest, _a.WorkspaceId)).Status.Should().Be(HttpStatusCode.Forbidden);
        (await GetAsync(_b.User, _a.WorkspaceId)).Status.Should().Be(HttpStatusCode.NotFound);
    }
}
