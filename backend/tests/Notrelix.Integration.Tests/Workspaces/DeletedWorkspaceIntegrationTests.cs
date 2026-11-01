using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.Workspaces.DTOs;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetDeletedWorkspaces;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.CrossContext.Identity.Avatars;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Identity.Services;
using Notrelix.Infrastructure.Workspaces.Members;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;
using BusinessRule = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Workspace deletion, the "Recently deleted" list, restore and the 30-day purge on real PostgreSQL with
/// migrations AND the RLS policy pack applied, executed as non-superuser logins of the application and worker
/// roles (FORCE ROW LEVEL SECURITY applies). Owner decision 2026-11-01: a deleted workspace is visible only to
/// its Owner (RLS narrows, never widens), restorable inside the window, then purged with an audit fact.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DeletedWorkspaceIntegrationTests : IAsyncLifetime
{
    private static readonly Guid AccountA = Guid.Parse("C0000000-0000-0000-0000-0000000000A1");
    private static readonly Guid AccountB = Guid.Parse("D0000000-0000-0000-0000-0000000000B2");

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_deleted_workspaces")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private string _appConnectionString = string.Empty;
    private string _workerConnectionString = string.Empty;
    private readonly Mock<IAuditService> _audit = new();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }

        _appConnectionString = await AppRoleLogin.CreateAsync(
            _container.GetConnectionString(), "deleted_ws_app_login", AppRoleLogin.ApplicationRole);
        _workerConnectionString = await AppRoleLogin.CreateAsync(
            _container.GetConnectionString(), "deleted_ws_worker_login", AppRoleLogin.WorkerRole);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    // ───────────────────────── delete, hide, restore ─────────────────────────

    [Fact]
    public async Task Delete_ThenRestore_HidesTheRowFromEveryoneButItsOwner_AndRestoreBringsItBackAsItWas()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var stranger = await SeedUserAsync("stranger");
        var workspace = await SeedWorkspaceAsync(AccountA, owner, "Acme");
        await AddMemberAsync(AccountA, owner, workspace, member, WorkspaceRole.Member);
        await SeedAccountMemberAsync(AccountA, stranger);
        var otherOwner = await SeedUserAsync("other-owner");
        var otherWorkspace = await SeedWorkspaceAsync(AccountB, otherOwner, "Other");

        (await VisibleToAsync(member, AccountA, workspace)).Should().Be(1, "an active workspace is visible to its members");

        var version = await VersionAsync(workspace);
        await DeleteAsync(owner, AccountA, workspace, version);

        (await VisibleToAsync(owner, AccountA, workspace)).Should().Be(1, "the Owner still sees it, to restore it");
        (await VisibleToAsync(member, AccountA, workspace)).Should().Be(0, "a member loses it at once");
        (await VisibleToAsync(stranger, AccountA, workspace)).Should().Be(0, "an Account member who never joined it never sees it");
        (await VisibleToAsync(otherOwner, AccountB, workspace)).Should().Be(0, "another tenant never sees it");
        (await VisibleToAsync(otherOwner, AccountB, otherWorkspace)).Should().Be(1, "another tenant's own workspace is untouched");
        (await WorkerVisibleAsync(workspace)).Should().Be(1, "the worker keeps the row so the purge can find it");

        var deletedVersion = await VersionAsync(workspace);
        deletedVersion.Should().Be(version + 1);
        await RestoreAsync(owner, AccountA, workspace, deletedVersion);

        (await VisibleToAsync(member, AccountA, workspace)).Should().Be(1, "everything comes back as it was");
        (await VersionAsync(workspace)).Should().Be(deletedVersion + 1);
        (await MembershipCountAsync(workspace)).Should().Be(2, "the memberships were never touched");
    }

    [Fact]
    public async Task ANonOwner_CannotDeleteOrRestoreAWorkspace_EvenWithRawSqlOnTheApplicationRole()
    {
        var owner = await SeedUserAsync("owner");
        var admin = await SeedUserAsync("admin");
        var workspace = await SeedWorkspaceAsync(AccountA, owner, "Acme");
        await AddMemberAsync(AccountA, owner, workspace, admin, WorkspaceRole.Admin);

        var deleteAsAdmin = async () => await AsAppAsync(admin, AccountA, workspace,
            $"UPDATE workspace.workspaces SET deleted_at = now(), deleted_by = '{admin}' WHERE id = '{workspace}'");
        (await deleteAsAdmin.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("42501");

        await DeleteAsync(owner, AccountA, workspace, await VersionAsync(workspace));
        (await AsAppAsync(admin, AccountA, workspace,
            $"UPDATE workspace.workspaces SET deleted_at = NULL, deleted_by = NULL WHERE id = '{workspace}'"))
            .Should().Be(0, "the deleted row is invisible to the Admin, so there is nothing to update");
        (await DeletedAtAsync(workspace)).Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WithAStaleVersion_IsRefused_AndTheWorkspaceStaysActive()
    {
        var owner = await SeedUserAsync("owner");
        var workspace = await SeedWorkspaceAsync(AccountA, owner, "Acme");
        var version = await VersionAsync(workspace);

        var act = () => DeleteAsync(owner, AccountA, workspace, version + 5);

        await act.Should().ThrowAsync<PreconditionFailedException>();
        (await DeletedAtAsync(workspace)).Should().BeNull();
        (await VersionAsync(workspace)).Should().Be(version);
    }

    [Fact]
    public async Task Delete_OfAPersonalWorkspace_IsRefused()
    {
        var owner = await SeedUserAsync("owner");
        var personal = await SeedWorkspaceAsync(AccountA, owner, "Personal", isPersonal: true);

        var act = async () => await DeleteAsync(owner, AccountA, personal, await VersionAsync(personal));

        (await act.Should().ThrowAsync<BusinessRule>()).Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_CannotDeletePersonal);
        (await DeletedAtAsync(personal)).Should().BeNull();
    }

    [Fact]
    public async Task Restore_AfterTheThirtyDayWindow_IsRefused_AndTheWorkspaceStaysDeleted()
    {
        var owner = await SeedUserAsync("owner");
        var workspace = await SeedWorkspaceAsync(AccountA, owner, "Acme");
        await DeleteAsync(owner, AccountA, workspace, await VersionAsync(workspace));
        await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '31 days' WHERE id = '{workspace}'");

        var act = async () => await RestoreAsync(owner, AccountA, workspace, await VersionAsync(workspace));

        (await act.Should().ThrowAsync<BusinessRule>()).Which.RuleCode
            .Should().Be(WorkspaceRuleCodes.Workspaces_Workspace_RestoreWindowExpired);
        (await DeletedAtAsync(workspace)).Should().NotBeNull();
    }

    [Fact]
    public async Task RecentlyDeleted_ListsOnlyTheCallersOwnedWorkspacesInsideTheWindow_WithTheirPurgeDate()
    {
        var owner = await SeedUserAsync("owner");
        var member = await SeedUserAsync("member");
        var inside = await SeedWorkspaceAsync(AccountA, owner, "Inside");
        var outside = await SeedWorkspaceAsync(AccountA, owner, "Outside");
        var active = await SeedWorkspaceAsync(AccountA, owner, "Active");
        await AddMemberAsync(AccountA, owner, inside, member, WorkspaceRole.Member);
        await DeleteAsync(owner, AccountA, inside, await VersionAsync(inside));
        await DeleteAsync(owner, AccountA, outside, await VersionAsync(outside));
        await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '31 days' WHERE id = '{outside}'");

        var asOwner = await ListDeletedAsync(owner, AccountA);
        var asMember = await ListDeletedAsync(member, AccountA);
        var otherTenant = await ListDeletedAsync(await SeedUserAsync("elsewhere"), AccountB);

        var entry = asOwner.Should().ContainSingle().Which;
        entry.Id.Should().Be(inside);
        entry.Name.Should().Be("Inside");
        entry.PurgeAfter.Should().Be(entry.DeletedAt.AddDays(30));
        entry.Version.Should().Be(await VersionAsync(inside));
        asMember.Should().BeEmpty("only an Owner sees the list");
        otherTenant.Should().BeEmpty();
        active.Should().NotBeEmpty();
    }

    // ───────────────────────── purge ─────────────────────────

    [Fact]
    public async Task Purge_RemovesTheDataOfAnExpiredWorkspaceOnly_WritesOneAuditFact_AndASecondPassChangesNothing()
    {
        var owner = await SeedUserAsync("owner");
        var expired = await SeedWorkspaceAsync(AccountA, owner, "Expired");
        var recent = await SeedWorkspaceAsync(AccountA, owner, "Recent");
        var active = await SeedWorkspaceAsync(AccountA, owner, "Active");
        foreach (var workspace in new[] { expired, recent, active })
            await SeedBoardWithItemAsync(AccountA, workspace, owner);
        await DeleteAsync(owner, AccountA, expired, await VersionAsync(expired));
        await DeleteAsync(owner, AccountA, recent, await VersionAsync(recent));
        await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '31 days' WHERE id = '{expired}'");
        await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '29 days' WHERE id = '{recent}'");
        (await ScopedRowCountAsync(expired)).Should().BeGreaterThan(5, "the workspace holds a board, a group, an item, a member and a grant");
        var recentRows = await ScopedRowCountAsync(recent);
        var activeRows = await ScopedRowCountAsync(active);

        var first = await PurgeAsync(batchSize: 10);
        var second = await PurgeAsync(batchSize: 10);

        first.Should().Be(1, "only the workspace deleted more than 30 days ago");
        second.Should().Be(0, "a purged workspace is gone and a second pass has nothing to claim");
        (await ScopedRowCountAsync(expired)).Should().Be(0);
        (await WorkspaceRowAsync(expired)).Should().Be(0);
        (await WorkspaceRowAsync(recent)).Should().Be(1, "inside the window it can still be restored");
        (await ScopedRowCountAsync(recent)).Should().Be(recentRows);
        (await ScopedRowCountAsync(active)).Should().Be(activeRows);
        (await OwnerScalarAsync(
            $"SELECT count(*) FROM audit.audit_logs WHERE action = 'PurgeWorkspace' AND workspace_id = '{expired}' AND actor_type = 'System' AND severity = 'Critical'"))
            .Should().Be("1", "one audit fact per purged workspace, kept after the data is gone");
    }

    [Fact]
    public async Task Purge_OfAFailingWorkspace_RollsBackWholly_AndTheNextPassRetriesIt()
    {
        var owner = await SeedUserAsync("owner");
        var expired = await SeedWorkspaceAsync(AccountA, owner, "Expired");
        await SeedBoardWithItemAsync(AccountA, expired, owner);
        await DeleteAsync(owner, AccountA, expired, await VersionAsync(expired));
        await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '31 days' WHERE id = '{expired}'");
        var rows = await ScopedRowCountAsync(expired);
        // A blocker that makes the final workspace-row delete fail after the child tables were emptied.
        await OwnerSqlAsync("""
            CREATE FUNCTION workspace.refuse_purge() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'blocked'; END $$;
            CREATE TRIGGER refuse_purge BEFORE DELETE ON workspace.workspaces FOR EACH ROW EXECUTE FUNCTION workspace.refuse_purge();
            """);

        var failing = () => PurgeAsync(batchSize: 10);

        await failing.Should().ThrowAsync<Exception>();
        (await ScopedRowCountAsync(expired)).Should().Be(rows, "the failed pass rolled every table back");
        (await OwnerScalarAsync($"SELECT count(*) FROM audit.audit_logs WHERE action = 'PurgeWorkspace' AND workspace_id = '{expired}'")).Should().Be("0");

        await OwnerSqlAsync("DROP TRIGGER refuse_purge ON workspace.workspaces");
        (await PurgeAsync(batchSize: 10)).Should().Be(1, "the retry purges it");
        (await ScopedRowCountAsync(expired)).Should().Be(0);
    }

    [Fact]
    public async Task Purge_HonoursTheBatchSize_AndOptionsAreValidated()
    {
        var owner = await SeedUserAsync("owner");
        var ids = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            var workspace = await SeedWorkspaceAsync(AccountA, owner, $"Expired {i}");
            await DeleteAsync(owner, AccountA, workspace, await VersionAsync(workspace));
            await OwnerSqlAsync($"UPDATE workspace.workspaces SET deleted_at = now() - interval '40 days' WHERE id = '{workspace}'");
            ids.Add(workspace);
        }

        (await PurgeAsync(batchSize: 2)).Should().Be(2);
        (await PurgeAsync(batchSize: 2)).Should().Be(1);

        var validator = new DeletedWorkspacePurgeOptionsValidator();
        validator.Validate(null, new DeletedWorkspacePurgeOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new DeletedWorkspacePurgeOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        validator.Validate(null, new DeletedWorkspacePurgeOptions { BatchSize = 0 }).Failed.Should().BeTrue();
        new DeletedWorkspacePurgeOptions().Interval.Should().Be(TimeSpan.FromHours(1));
    }

    // ───────────────────────── harness ─────────────────────────

    private static readonly DateTimeOffset SeedTime = DateTimeOffset.UtcNow;

    private static Mock<ICurrentRequestContext> RequestOf(Guid userId, Guid accountId, Guid? workspaceId = null)
    {
        var request = new Mock<ICurrentRequestContext>();
        request.Setup(r => r.UserId).Returns(userId);
        request.Setup(r => r.IsAuthenticated).Returns(true);
        request.Setup(r => r.RequireAccountId()).Returns(accountId);
        if (workspaceId is { } w) request.Setup(r => r.RequireWorkspaceId()).Returns(w);
        return request;
    }

    private static IDateTimeProvider Clock()
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(() => DateTimeOffset.UtcNow);
        return clock.Object;
    }

    private static FakeCurrentTenantContext System()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private async Task<Guid> SeedUserAsync(string name)
    {
        await using var context = CreateContext(System());
        var user = User.Create($"{name}.{Guid.NewGuid():N}@example.test", name, "hash", SeedTime, hasPasswordCredential: true);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    private async Task SeedAccountMemberAsync(Guid accountId, Guid userId)
    {
        // Account membership grant only (no workspace): what the Account's members hold before joining a workspace.
        await using var context = CreateContext(System());
        context.AccessGrants.Add(new AccessGrant(
            accountId: accountId, workspaceId: null, userId: userId, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: ["Member"], permissionCodes: [], isAccountAdmin: false,
            isWorkspaceAdmin: false, grantedAt: SeedTime));
        await context.SaveChangesAsync();
    }

    private async Task<Guid> SeedWorkspaceAsync(Guid accountId, Guid creator, string name, bool isPersonal = false)
    {
        await using var context = CreateContext(System());
        var handler = new CreateWorkspaceCommandHandler(
            context, RequestOf(creator, accountId).Object, Clock(),
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));
        var created = await handler.Handle(new CreateWorkspaceCommand(name, null, isPersonal), default);
        created.Succeeded.Should().BeTrue(string.Join("; ", created.Errors));
        await context.SaveChangesAsync();
        return created.Data;
    }

    private async Task AddMemberAsync(Guid accountId, Guid actor, Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        await using var context = CreateContext(System());
        var handler = new AddMemberCommandHandler(
            context, RequestOf(actor, accountId).Object, Clock(),
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)),
            Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)),
            Mock.Of<Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipFacts>(f => f.IsActiveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)));
        (await handler.Handle(new AddMemberCommand(workspaceId, userId, role), default)).Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
    }

    private async Task SeedBoardWithItemAsync(Guid accountId, Guid workspaceId, Guid creator)
    {
        await using var context = CreateContext(System());
        var board = Board.Create(accountId, workspaceId, creator, "Roadmap", null, SeedTime);
        var group = BoardGroup.Create(accountId, workspaceId, board.Id, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), creator, SeedTime);
        context.Boards.Add(board);
        context.BoardGroups.Add(group);
        context.BoardItems.Add(BoardItem.CreateRoot(accountId, workspaceId, board.Id, group.Id, "First", FractionalIndex.Create("a1"), creator, SeedTime));
        await context.SaveChangesAsync();
    }

    /// <summary>Runs one request through the real data session as the application-role login.</summary>
    private async Task<T> RunAsync<T>(
        Guid userId, Guid accountId, Guid? workspaceId, ExpectedVersionConstraint? version,
        Func<ApplicationDbContext, ICurrentRequestContext, Task<T>> action)
    {
        var tenant = new FakeCurrentTenantContext();
        if (workspaceId is { } w) tenant.SetWorkspace(accountId, w, userId);
        else tenant.SetAccount(accountId, userId);

        await using var context = CreateContext(tenant, _appConnectionString);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var session = new EfRequestDataSession(context, rls, NullLogger<EfRequestDataSession>.Instance);
        return await session.ExecuteAsync(
            new RequestDataSessionOptions(RequestDataAccess.Transactional, true, false, version),
            async _ => await action(context, RequestOf(userId, accountId, workspaceId).Object),
            default);
    }

    private static ExpectedVersionConstraint Constraint(Type request, Guid workspace, long version) =>
        new(request, ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), workspace, workspace), version);

    private Task DeleteAsync(Guid userId, Guid accountId, Guid workspace, long version) =>
        RunAsync<bool>(userId, accountId, workspace, Constraint(typeof(DeleteWorkspaceCommand), workspace, version), async (context, request) =>
        {
            var handler = new DeleteWorkspaceCommandHandler(context, request, Clock(), _audit.Object);
            return (await handler.Handle(new DeleteWorkspaceCommand(workspace, version), default)).Succeeded;
        });

    private Task RestoreAsync(Guid userId, Guid accountId, Guid workspace, long version) =>
        RunAsync<bool>(userId, accountId, workspace, Constraint(typeof(RestoreWorkspaceCommand), workspace, version), async (context, request) =>
        {
            var handler = new RestoreWorkspaceCommandHandler(context, request, Clock(), _audit.Object);
            return (await handler.Handle(new RestoreWorkspaceCommand(workspace, version), default)).Succeeded;
        });

    private Task<List<DeletedWorkspaceDto>> ListDeletedAsync(Guid userId, Guid accountId) =>
        RunAsync(userId, accountId, null, null, async (context, request) =>
        {
            var handler = new GetDeletedWorkspacesQueryHandler(context, request, Clock());
            var result = await handler.Handle(new GetDeletedWorkspacesQuery(), default);
            result.Succeeded.Should().BeTrue();
            return result.Data!;
        });

    private async Task<int> PurgeAsync(int batchSize)
    {
        var tenant = new CurrentTenantContext();
        await using var context = CreateContext(tenant, _workerConnectionString);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var purger = new DeletedWorkspacePurger(
            context, tenant, rls, Clock(),
            Options.Create(new DeletedWorkspacePurgeOptions { BatchSize = batchSize }),
            NullLogger<DeletedWorkspacePurger>.Instance);
        return await purger.RunOnceAsync(default);
    }

    /// <summary>The number of rows the application login can read for a workspace, inside the caller's own scope.</summary>
    private async Task<long> VisibleToAsync(Guid userId, Guid accountId, Guid workspace)
    {
        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetScopeAsync(connection, transaction, userId, accountId, workspace);
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM workspace.workspaces WHERE id = '{workspace}'", connection, transaction);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> WorkerVisibleAsync(Guid workspace)
    {
        await using var connection = new NpgsqlConnection(_workerConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand("SELECT set_config('app.request_scope', 'worker', true)", connection, transaction))
            await scope.ExecuteNonQueryAsync();
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM workspace.workspaces WHERE id = '{workspace}'", connection, transaction);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Runs a statement as the application login in the caller's scope; returns the rows it changed.</summary>
    private async Task<int> AsAppAsync(Guid userId, Guid accountId, Guid workspace, string sql)
    {
        await using var connection = new NpgsqlConnection(_appConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await SetScopeAsync(connection, transaction, userId, accountId, workspace);
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        var changed = await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return changed;
    }

    private static async Task SetScopeAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid userId, Guid accountId, Guid workspace)
    {
        await using var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction);
        scope.Parameters.AddWithValue("u", userId.ToString());
        scope.Parameters.AddWithValue("a", accountId.ToString());
        scope.Parameters.AddWithValue("w", workspace.ToString());
        await scope.ExecuteNonQueryAsync();
    }

    private async Task<long> VersionAsync(Guid workspace) => long.Parse(await OwnerScalarAsync($"SELECT version FROM workspace.workspaces WHERE id = '{workspace}'"));

    private async Task<DateTimeOffset?> DeletedAtAsync(Guid workspace)
    {
        var raw = await OwnerScalarAsync($"SELECT deleted_at FROM workspace.workspaces WHERE id = '{workspace}'");
        return string.IsNullOrEmpty(raw) ? null : DateTimeOffset.Parse(raw);
    }

    private async Task<long> WorkspaceRowAsync(Guid workspace) => long.Parse(await OwnerScalarAsync($"SELECT count(*) FROM workspace.workspaces WHERE id = '{workspace}'"));

    private async Task<long> MembershipCountAsync(Guid workspace) =>
        long.Parse(await OwnerScalarAsync($"SELECT count(*) FROM workspace.workspace_members WHERE workspace_id = '{workspace}' AND status = 'Active'"));

    /// <summary>Rows of every table outside the retained schemas that carry this workspace's id (plus the workspace row).</summary>
    private async Task<long> ScopedRowCountAsync(Guid workspace)
    {
        var tables = new List<string>();
        await using (var connection = new NpgsqlConnection(_container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("""
                SELECT format('%I.%I', n.nspname, c.relname)
                FROM pg_attribute a JOIN pg_class c ON c.oid = a.attrelid AND c.relkind IN ('r','p')
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE a.attname = 'workspace_id' AND NOT a.attisdropped AND a.atttypid = 'uuid'::regtype
                  AND n.nspname NOT IN ('audit','billing','messaging','events','ops','operators')
                  AND n.nspname NOT LIKE 'pg\_%' AND n.nspname <> 'information_schema'
                """, connection);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add(reader.GetString(0));
        }

        var total = await WorkspaceRowAsync(workspace);
        foreach (var table in tables)
            total += long.Parse(await OwnerScalarAsync($"SELECT count(*) FROM {table} WHERE workspace_id = '{workspace}'"));
        return total;
    }

    private async Task<string> OwnerScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? string.Empty;
    }

    private async Task OwnerSqlAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null, string? connectionString = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString ?? _container.GetConnectionString(), npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return tenant is not null ? new ApplicationDbContext(options, tenant) : new ApplicationDbContext(options);
    }
}
