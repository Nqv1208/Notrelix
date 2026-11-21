using Microsoft.Extensions.Options;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Workspaces.Navigation;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The pin write path as the application login: it loads and deletes only the caller's own pins in the caller's own
/// Workspace, so pruning unreadable pins with a new pin can never reach another member's or another tenant's rows.
/// Needs Docker locally.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class WorkspaceNavigationPinRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _colleague = null!;

    public WorkspaceNavigationPinRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "nav-pin-a@example.com", "NavA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "nav-pin-b@example.com", "NavB", "hash");
        _colleague = await AppRoleMembers.AddAsync(_fixture, _a, "NavColleague", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.PinnedResources.AddRange(
            Pin(_a, _a.User.Id, _a.BoardId, 0),
            Pin(_a, _a.User.Id, Guid.CreateVersion7(), 1),
            Pin(_a, _a.User.Id, Guid.CreateVersion7(), 2),
            Pin(_a, _colleague.Id, Guid.CreateVersion7(), 0),
            Pin(_b, _b.User.Id, Guid.CreateVersion7(), 0));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static PinnedResource Pin(AppRoleTenant tenant, Guid userId, Guid boardId, int count) =>
        PinnedResource.Create(tenant.AccountId, tenant.WorkspaceId, userId, NavigationResourceKinds.Board, boardId, count, Now);

    private async Task<int> OwnerCountAsync()
    {
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        return await ctx.PinnedResources.CountAsync();
    }

    [Fact]
    public async Task TheMemberLoadsAndDeletesOnlyTheirOwnPins_NeverAColleaguesOrAnotherTenants()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_a.AccountId, _a.WorkspaceId, _a.User.Id);
        await using (var db = _fixture.CreateAppContext(tenant))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), tenant).ApplyAsync(default);
            var store = new WorkspaceNavigationStore(db);
            var mine = await store.LoadPinsAsync(_a.WorkspaceId, _a.User.Id, CancellationToken.None);
            mine.Should().HaveCount(3);
            (await db.PinnedResources.CountAsync()).Should().Be(3, "the colleague's and the other tenant's pins are invisible to this login");

            foreach (var stale in mine.Where(p => p.ResourceId != _a.BoardId))
                store.RemovePin(stale);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        (await OwnerCountAsync()).Should().Be(3, "one own pin, the colleague's pin and the other tenant's pin remain");
    }

    [Fact]
    public async Task AnotherTenant_LoadsNoneOfThisMembersPins()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_b.AccountId, _b.WorkspaceId, _b.User.Id);
        await using var db = _fixture.CreateAppContext(tenant);
        await using var tx = await db.Database.BeginTransactionAsync();
        await new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), tenant).ApplyAsync(default);

        var store = new WorkspaceNavigationStore(db);
        (await store.LoadPinsAsync(_a.WorkspaceId, _a.User.Id, CancellationToken.None)).Should().BeEmpty();
    }
}
