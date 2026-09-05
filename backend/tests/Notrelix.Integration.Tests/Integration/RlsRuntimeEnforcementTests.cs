using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.SharedKernel;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Domain.Builders;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// FZ-INF-02 — RLS provider certification at runtime.
///
/// A dedicated PostgreSQL container with migrations AND the RLS policy pack
/// applied (the shared Integration container does not apply policies). Business
/// rows are seeded as the superuser; access is then probed through a raw
/// connection that SET ROLEs to the application role and sets the session
/// context exactly as RlsSessionContext does in production.
/// </summary>
public sealed class RlsRuntimeEnforcementTests : IAsyncLifetime
{
    private static readonly Guid AccountA = Guid.Parse("A0000000-0000-0000-0000-000000000001");
    private static readonly Guid AccountB = Guid.Parse("B0000000-0000-0000-0000-000000000002");
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-0000000000AA");
    private static readonly Guid WsA1 = Guid.Parse("A0000000-0000-0000-0000-00000000AA01");
    private static readonly Guid WsA2 = Guid.Parse("A0000000-0000-0000-0000-00000000AA02");
    private static readonly Guid WsB1 = Guid.Parse("B0000000-0000-0000-0000-00000000BB01");
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("notrelix_rls_runtime")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplyMigrationsAndRlsAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>();

        var options = optionsBuilder.Options;
        return tenant is not null
            ? new ApplicationDbContext(options, tenant)
            : new ApplicationDbContext(options);
    }

    private async Task ApplyMigrationsAndRlsAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var applier = new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance);
        await applier.ApplyAsync();
    }

    private async Task SeedBoardsAsync()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Boards.AddRange(
            new BoardBuilder().WithAccountId(AccountA).WithWorkspaceId(Guid.Parse("A0000000-0000-0000-0000-00000000AA01")).WithCreatedBy(UserId).WithTitle("Board A1").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountA).WithWorkspaceId(Guid.Parse("A0000000-0000-0000-0000-00000000AA02")).WithCreatedBy(UserId).WithTitle("Board A2").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountB).WithWorkspaceId(Guid.Parse("B0000000-0000-0000-0000-00000000BB01")).WithCreatedBy(UserId).WithTitle("Board B1").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();
    }

    private async Task SeedPagesAndCommentsAsync()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Pages.AddRange(
            Page.Create(AccountA, WsA1, "Page A1", UserId, FixedTime),
            Page.Create(AccountA, WsA2, "Page A2", UserId, FixedTime),
            Page.Create(AccountB, WsB1, "Page B1", UserId, FixedTime));

        context.Comments.AddRange(
            Comment.Create(AccountA, WsA1, ResourceRef.Create(ResourceKind.Create("work.board-item"), Guid.Parse("A0000000-0000-0000-0000-00000000AA11"), WsA1), "Comment A1", UserId, FixedTime),
            Comment.Create(AccountB, WsB1, ResourceRef.Create(ResourceKind.Create("work.board-item"), Guid.Parse("B0000000-0000-0000-0000-00000000BB11"), WsB1), "Comment B1", UserId, FixedTime));

        await context.SaveChangesAsync();
    }

    private async Task SeedGrantAsync(Guid accountId, Guid? workspaceId, Guid? userId = null)
    {
        await using var context = CreateContext();

        context.AccessGrants.Add(new AccessGrant(
            accountId: accountId,
            workspaceId: workspaceId,
            userId: userId ?? UserId,
            sourceContext: "Workspace",
            membershipStatus: "Active",
            roleCodes: [],
            permissionCodes: [],
            isAccountAdmin: false,
            isWorkspaceAdmin: false,
            grantedAt: FixedTime));

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Executes SQL as the application role with the production session context
    /// (app.current_user_id / request_scope). The statements run inside one
    /// explicit transaction so the transaction-local set_config values apply to
    /// the query. RLS enforcement on workspace-scoped rows is per-row: the
    /// policy calls ops.has_workspace_access(row.account_id, row.workspace_id),
    /// which is granted by the authz.access_grants of the CURRENT user.
    /// </summary>
    private async Task<List<string>> QueryBoardTitlesAsAppRoleAsync(
        Guid? userId = null,
        string requestScope = "app")
    {
        return await QueryScalarsAsAppRoleAsync(
            "SELECT title FROM work.boards ORDER BY title", userId, requestScope);
    }

    private async Task<List<string>> QueryScalarsAsAppRoleAsync(
        string selectSql,
        Guid? userId = null,
        string requestScope = "app")
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        var userIdSql = userId is null ? "NULL" : $"'{userId}'::uuid";

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            BEGIN;
            SET ROLE notrelix_app;
            DO $$
            BEGIN
                PERFORM set_config('app.current_user_id', {userIdSql}::text, true);
                PERFORM set_config('app.request_scope', '{requestScope}', true);
            END
            $$;
            {selectSql};
            COMMIT;
            """;

        var values = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }
        }

        return values;
    }

    [Fact]
    public async Task AppRole_MissingSessionContext_FailsClosed_SeesNoRows()
    {
        await SeedBoardsAsync();

        var titles = await QueryBoardTitlesAsAppRoleAsync();

        titles.Should().BeEmpty("a request without user context must never see workspace data");
    }

    [Fact]
    public async Task AppRole_NoGrant_FailsClosed_SeesNoRows()
    {
        await SeedBoardsAsync();

        var titles = await QueryBoardTitlesAsAppRoleAsync(userId: UserId);

        titles.Should().BeEmpty("an authenticated user without an access grant must see nothing");
    }

    [Fact]
    public async Task AppRole_CrossAccount_GrantInOneAccount_DoesNotSeeOtherAccount()
    {
        await SeedBoardsAsync();
        var userA = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
        var userB = Guid.Parse("00000000-0000-0000-0000-0000000000B1");
        await SeedGrantAsync(AccountA, Guid.Parse("A0000000-0000-0000-0000-00000000AA01"), userA);
        await SeedGrantAsync(AccountB, Guid.Parse("B0000000-0000-0000-0000-00000000BB01"), userB);

        var titlesOfUserA = await QueryBoardTitlesAsAppRoleAsync(userId: userA);
        titlesOfUserA.Should().BeEquivalentTo(["Board A1"],
            "a user with a grant in account A must not see account B rows");

        var titlesOfUserB = await QueryBoardTitlesAsAppRoleAsync(userId: userB);
        titlesOfUserB.Should().BeEquivalentTo(["Board B1"],
            "a user with a grant in account B must not see account A rows");
    }

    [Fact]
    public async Task AppRole_CrossWorkspace_GrantInOneWorkspace_DoesNotSeeOtherWorkspace()
    {
        await SeedBoardsAsync();
        await SeedGrantAsync(AccountA, Guid.Parse("A0000000-0000-0000-0000-00000000AA01"));

        var titles = await QueryBoardTitlesAsAppRoleAsync(userId: UserId);

        titles.Should().BeEquivalentTo(["Board A1"],
            "a grant in workspace AA01 must not reach the AA02 board of the same account");
    }

    [Fact]
    public async Task AppRole_GrantedUser_SeesExactlyOwnRows()
    {
        await SeedBoardsAsync();
        await SeedGrantAsync(AccountA, Guid.Parse("A0000000-0000-0000-0000-00000000AA01"));

        var titles = await QueryBoardTitlesAsAppRoleAsync(userId: UserId);

        titles.Should().BeEquivalentTo(["Board A1"]);
    }

    [Fact]
    public async Task WorkerAndSystemScopes_BypassWorkspacePolicies_SeeAll()
    {
        await SeedBoardsAsync();

        var asWorker = await QueryBoardTitlesAsAppRoleAsync(requestScope: "worker");
        asWorker.Should().HaveCount(3, "worker is the explicit background bypass scope");

        var asSystem = await QueryBoardTitlesAsAppRoleAsync(requestScope: "system");
        asSystem.Should().HaveCount(3, "system is the explicit system-context bypass scope");
    }

    [Fact]
    public async Task BackgroundScope_NoGrant_FailsClosed()
    {
        await SeedBoardsAsync();

        var titles = await QueryBoardTitlesAsAppRoleAsync(userId: UserId, requestScope: "background");

        titles.Should().BeEmpty("a background consumer without a grant must not inherit any bypass");
    }

    [Fact]
    public async Task BackgroundScope_WithGrant_SeesOwnRowsOnly_NoBypass()
    {
        await SeedBoardsAsync();
        await SeedGrantAsync(AccountA, WsA1);

        var titles = await QueryBoardTitlesAsAppRoleAsync(userId: UserId, requestScope: "background");

        titles.Should().BeEquivalentTo(["Board A1"],
            "background is not a worker scope; grants still apply row by row");
    }

    /// <summary>
    /// IA-PLAN-STOP-015 resolution proof: a membership mutation executed through the
    /// production handler + AccessGrantProjectionService writes authz.access_grants
    /// synchronously, and the RLS predicate built from that grant is enforced for the
    /// application role — the member sees the new workspace rows, an unrelated user
    /// sees nothing.
    /// </summary>
    [Fact]
    public async Task RuntimeMembershipCreation_WritesGrant_AndEnforcesUnderAppRole()
    {
        var creatorId = Guid.Parse("00000000-0000-0000-0000-000000000C01");
        var unrelatedId = Guid.Parse("00000000-0000-0000-0000-000000000C02");

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(creatorId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(AccountA);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedTime);

        var handler = new CreateWorkspaceCommandHandler(
            context, requestContext.Object, clock.Object, new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)));

        var result = await handler.Handle(new CreateWorkspaceCommand("Runtime Grant Workspace", null, false), default);
        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == AccountA && g.WorkspaceId == result.Data && g.UserId == creatorId);
        grant.MembershipStatus.Should().Be("Active");
        grant.RevokedAt.Should().BeNull();
        grant.IsWorkspaceAdmin.Should().BeTrue("the workspace creator is the Owner");

        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountA)
            .WithWorkspaceId(result.Data)
            .WithCreatedBy(creatorId)
            .WithTitle("Runtime Board")
            .WithCreatedAt(FixedTime)
            .Build());
        await context.SaveChangesAsync();

        var creatorTitles = await QueryBoardTitlesAsAppRoleAsync(userId: creatorId);
        creatorTitles.Should().BeEquivalentTo(["Runtime Board"],
            "the runtime-written grant must give the member visibility under the enforced app role");

        var unrelatedTitles = await QueryBoardTitlesAsAppRoleAsync(userId: unrelatedId);
        unrelatedTitles.Should().BeEmpty(
            "a user without a runtime-written grant must see nothing under the enforced app role");
    }

    [Fact]
    public async Task DocsPagesAndCollabComments_CrossAccount_GrantInOneAccount()
    {
        await SeedPagesAndCommentsAsync();
        await SeedGrantAsync(AccountA, WsA1);

        var pageTitles = await QueryScalarsAsAppRoleAsync("SELECT title FROM docs.pages ORDER BY title", userId: UserId);
        pageTitles.Should().BeEquivalentTo(["Page A1"],
            "docs.pages must be workspace-scoped by account/workspace");

        var contents = await QueryScalarsAsAppRoleAsync("SELECT content #>> '{}' FROM collab.comments ORDER BY content #>> '{}'", userId: UserId);
        contents.Should().BeEquivalentTo(["Comment A1"],
            "collab.comments must be workspace-scoped by account/workspace");
    }

    [Fact]
    public async Task SupportReadonlyRole_SelectsAllRows_ButCannotMutate()
    {
        await SeedBoardsAsync();

        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        await using (var role = connection.CreateCommand())
        {
            role.CommandText = "SET ROLE notrelix_support_readonly;";
            await role.ExecuteNonQueryAsync();
        }

        await using (var select = connection.CreateCommand())
        {
            select.CommandText = "SELECT title FROM work.boards ORDER BY title;";
            var titles = new List<string>();
            await using (var reader = await select.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    titles.Add(reader.GetString(0));
                }
            }

            titles.Should().HaveCount(3, "the support role reads everything through p_support_select");
        }

        await using (var update = connection.CreateCommand())
        {
            update.CommandText = "UPDATE work.boards SET title = title;";
            var updateAct = async () => await update.ExecuteNonQueryAsync();
            var updateException = await updateAct.Should().ThrowAsync<PostgresException>();
            updateException.Which.SqlState.Should().Be("42501",
                "the support role has SELECT-only grants; UPDATE must be denied at the privilege level");
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO work.boards (id, account_id, workspace_id, title, board_type, board_family, item_sequence, is_archived, created_at)
            VALUES ('C0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-00000000AA01', 'Board C1', 'Kanban', 'Standard', 0, false, '2026-06-28T00:00:00Z');
            """;
        var act = async () => await insert.ExecuteNonQueryAsync();
        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be("42501", "INSERT without an INSERT policy must be denied, not silently dropped");
    }

    [Fact]
    public async Task SessionContext_IsTransactionLocal_DoesNotLeakAfterCommit()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                BEGIN;
                SELECT set_config('app.current_workspace_id', 'A0000000-0000-0000-0000-000000000001', true);
                SELECT current_setting('app.current_workspace_id', true);
                COMMIT;
                """;
            var inside = (string)(await command.ExecuteScalarAsync())!;
            inside.Should().Be("A0000000-0000-0000-0000-000000000001");
        }

        await using var after = connection.CreateCommand();
        after.CommandText =
            "SELECT NULLIF(current_setting('app.current_workspace_id', true), '') IS NULL";
        var result = (bool)(await after.ExecuteScalarAsync())!;
        result.Should().BeTrue("transaction-local session settings must not leak into the pooled connection");
    }

    [Fact]
    public async Task SessionContext_IsTransactionLocal_DoesNotLeakAfterRollback()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                BEGIN;
                SELECT set_config('app.current_workspace_id', 'A0000000-0000-0000-0000-000000000001', true);
                ROLLBACK;
                """;
            await command.ExecuteNonQueryAsync();
        }

        await using var after = connection.CreateCommand();
        after.CommandText =
            "SELECT NULLIF(current_setting('app.current_workspace_id', true), '') IS NULL";
        var result = (bool)(await after.ExecuteScalarAsync())!;
        result.Should().BeTrue("a rolled-back request must not leave RLS context behind on the pooled connection");
    }

    [Fact]
    public async Task ApiTokens_NoGrant_FailsClosed()
    {
        await SeedApiTokensAsync();

        var names = await QueryScalarsAsAppRoleAsync(
            "SELECT name FROM identity.api_tokens ORDER BY name", userId: UserId);

        names.Should().BeEmpty("an authenticated user without an access grant must see no API tokens");
    }

    [Fact]
    public async Task ApiTokens_CrossWorkspace_GrantInOneWorkspace_DoesNotSeeOtherTokens()
    {
        await SeedApiTokensAsync();
        await SeedGrantAsync(AccountA, WsA1);

        var names = await QueryScalarsAsAppRoleAsync(
            "SELECT name FROM identity.api_tokens ORDER BY name", userId: UserId);

        names.Should().BeEquivalentTo(["Api Token A1"],
            "a grant in workspace AA01 must not reach the AA02 token of the same account");

        var crossAccountNames = await QueryScalarsAsAppRoleAsync(
            "SELECT name FROM identity.api_tokens ORDER BY name", userId: UserId);
        crossAccountNames.Should().NotContain("Api Token B1",
            "account B tokens must be invisible to an account A grant");
    }

    private async Task SeedApiTokensAsync()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.ApiTokens.AddRange(
            ApiToken.Create(AccountA, WsA1, UserId, "Api Token A1", "hash:token-a1", scopes: null, createdBy: UserId, createdAt: FixedTime, expiresAt: null),
            ApiToken.Create(AccountA, WsA2, UserId, "Api Token A2", "hash:token-a2", scopes: null, createdBy: UserId, createdAt: FixedTime, expiresAt: null),
            ApiToken.Create(AccountB, WsB1, UserId, "Api Token B1", "hash:token-b1", scopes: null, createdBy: UserId, createdAt: FixedTime, expiresAt: null));

        await context.SaveChangesAsync();
    }
}
