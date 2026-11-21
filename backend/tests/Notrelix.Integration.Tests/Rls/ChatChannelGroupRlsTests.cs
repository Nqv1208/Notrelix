using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Chat categories (UC-CHT-A02, A04) under the application role, which is subject to the Workspace row policy: a Workspace writes, renames and
/// deletes only its own categories; another Workspace reads none of them, its update and delete find no row, and it cannot plant one.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ChatChannelGroupRlsTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _groupId;

    public ChatChannelGroupRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "chat-grp-a@example.com", "GrpA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "chat-grp-b@example.com", "GrpB", "hash");

        var group = ChannelGroup.Create(_a.AccountId, _a.WorkspaceId, "Projects", 1, _a.User.Id, Now);
        _groupId = group.Id;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ChatChannelGroups.Add(group);
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>A request as the app login: one transaction with the Workspace session context applied, as the data session does.</summary>
    private async Task<(ApplicationDbContext Db, IDbContextTransaction Tx)> OpenAsync(AppRoleTenant tenant)
    {
        var context = new FakeCurrentTenantContext();
        context.SetWorkspace(tenant.AccountId, tenant.WorkspaceId, tenant.User.Id);
        var db = _fixture.CreateAppContext(context);
        var tx = await db.Database.BeginTransactionAsync();
        await new RlsSessionContext(db, Options.Create(new RlsOptions { SetSessionContext = true }), context).ApplyAsync(default);
        return (db, tx);
    }

    private async Task<string> NameAsAsync(AppRoleTenant tenant)
    {
        var (db, tx) = await OpenAsync(tenant);
        await using (db)
        await using (tx)
            return (await db.ChatChannelGroups.SingleAsync(g => g.Id == _groupId)).Name;
    }

    [Fact]
    public async Task TheOwningWorkspaceCreatesRenamesAndDeletesItsCategories_UnderTheAppRole()
    {
        var (db, tx) = await OpenAsync(_a);
        await using (db)
        await using (tx)
        {
            db.ChatChannelGroups.Add(ChannelGroup.Create(_a.AccountId, _a.WorkspaceId, "Teams", 2, _a.User.Id, Now));
            (await db.ChatChannelGroups.SingleAsync(g => g.Id == _groupId)).Update("Work", 5, _a.User.Id, Now);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        (await NameAsAsync(_a)).Should().Be("Work");

        var (db2, tx2) = await OpenAsync(_a);
        await using (db2)
        await using (tx2)
        {
            (await db2.ChatChannelGroups.CountAsync()).Should().Be(2);
            db2.ChatChannelGroups.Remove(await db2.ChatChannelGroups.SingleAsync(g => g.Id == _groupId));
            await db2.SaveChangesAsync();
            await tx2.CommitAsync();
        }

        var (db3, tx3) = await OpenAsync(_a);
        await using (db3)
        await using (tx3)
            (await db3.ChatChannelGroups.Select(g => g.Name).ToListAsync()).Should().Equal("Teams");
    }

    [Fact]
    public async Task AnotherWorkspaceReadsNoCategory_AndItsUpdateAndDeleteFindNoRow()
    {
        var (db, tx) = await OpenAsync(_b);
        await using (db)
        await using (tx)
        {
            (await db.ChatChannelGroups.CountAsync()).Should().Be(0, "the row policy hides the other Workspace's categories");
            (await db.ChatChannelGroups.Where(g => g.Id == _groupId).ExecuteUpdateAsync(s => s.SetProperty(g => g.Name, "Hijacked"))).Should().Be(0);
            (await db.ChatChannelGroups.Where(g => g.Id == _groupId).ExecuteDeleteAsync()).Should().Be(0);
            await tx.CommitAsync();
        }

        (await NameAsAsync(_a)).Should().Be("Projects");
    }

    [Fact]
    public async Task AnotherWorkspaceCannotPlantACategoryInsideTheFirstWorkspace()
    {
        var (db, tx) = await OpenAsync(_b);
        await using (db)
        await using (tx)
        {
            db.ChatChannelGroups.Add(ChannelGroup.Create(_a.AccountId, _a.WorkspaceId, "Planted", 3, _b.User.Id, Now));

            var act = () => db.SaveChangesAsync();

            await act.Should().ThrowAsync<Exception>("the policy refuses a row for a Workspace the caller is not in");
        }
    }
}
