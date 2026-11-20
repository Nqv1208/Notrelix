using Microsoft.Extensions.Options;
using Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;
using Notrelix.Infrastructure.Data.Collaboration;
using Notrelix.Infrastructure.Data.Projections.Activity;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The workspace activity feed as the application login: a tenant reads only its own workspace's facts, and asking for another
/// tenant's workspace id returns nothing. Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class WorkspaceActivityFeedRlsTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public WorkspaceActivityFeedRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "activity-a@example.com", "ActA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "activity-b@example.com", "ActB", "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.WorkspaceActivityLogs.AddRange(Fact(_a, 2), Fact(_a, 1), Fact(_b, 1));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static WorkspaceActivityLogRecord Fact(AppRoleTenant tenant, int minutesAgo) =>
        WorkspaceActivityLogRecord.Create(
            tenant.WorkspaceId, "work", "work.board-created", "Board", tenant.BoardId, DateTimeOffset.UtcNow.AddMinutes(-minutesAgo),
            sourceEventId: Guid.CreateVersion7(), actorUserId: tenant.User.Id, title: "board");

    private async Task<IReadOnlyList<WorkspaceActivityFact>> ReadAsAsync(AppRoleTenant caller, Guid workspaceId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(caller.AccountId, caller.WorkspaceId, caller.User.Id);
        await using var db = _fixture.CreateAppContext(tenant);
        await using var tx = await db.Database.BeginTransactionAsync();
        await new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), tenant).ApplyAsync(default);
        return await new WorkspaceActivityFeed(db).ListAsync(workspaceId, null, 50, CancellationToken.None);
    }

    [Fact]
    public async Task ATenantReadsItsOwnWorkspaceFacts_NewestFirst()
    {
        var facts = await ReadAsAsync(_a, _a.WorkspaceId);

        facts.Should().HaveCount(2);
        facts.Select(f => f.OccurredAt).Should().BeInDescendingOrder();
        facts.Should().OnlyContain(f => f.Scope.Kind == WorkspaceActivityScopeKind.Board && f.Scope.ResourceId == _a.BoardId);
    }

    [Fact]
    public async Task AnotherTenantsWorkspaceId_ReturnsNothing()
    {
        (await ReadAsAsync(_b, _a.WorkspaceId)).Should().BeEmpty("the other tenant's rows are invisible to this login even by id");
        (await ReadAsAsync(_b, _b.WorkspaceId)).Should().ContainSingle();
    }
}
