using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;
using Notrelix.Application.Features.Workspaces.Workspaces.Commands.CreateWorkspace;
using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Domain.Collaboration.Preferences;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Workspaces.Members;
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
        .WithImage("postgres:16.15-bookworm")
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
    public async Task TheWorkerScope_IsHonouredOnlyOnAWorkerLogin_AnApplicationLoginSettingItSeesNothing()
    {
        await SeedBoardsAsync();
        var app = await AppRoleLogin.CreateAsync(_container.GetConnectionString(), "rls_rt_app_login");
        var worker = await AppRoleLogin.CreateAsync(_container.GetConnectionString(), "rls_rt_worker_login", AppRoleLogin.WorkerRole);

        foreach (var scope in new[] { "worker", "system", "migration", "migrator" })
        {
            (await QueryAsLoginAsync(app, "SELECT title FROM work.boards", scope)).Should().BeEmpty(
                $"an application login that sets the '{scope}' scope gains nothing: the scope is not self-declarable");
        }

        (await QueryAsLoginAsync(worker, "SELECT title FROM work.boards", "worker")).Should().HaveCount(3,
            "the worker login is the explicit background principal and reads all tenants");
        (await QueryAsLoginAsync(worker, "SELECT ops.is_worker_scope()::text", "worker")).Should().Equal("true");
        (await QueryAsLoginAsync(app, "SELECT ops.is_worker_scope()::text", "worker")).Should().Equal("false");
    }

    /// <summary>Runs a query on a real (non-superuser) login with the given request scope set transaction-locally.</summary>
    private async Task<List<string>> QueryAsLoginAsync(string connectionString, string selectSql, string requestScope)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var set = new NpgsqlCommand($"SELECT set_config('app.request_scope', '{requestScope}', true)", connection, transaction))
            await set.ExecuteNonQueryAsync();

        var values = new List<string>();
        await using var command = new NpgsqlCommand(selectSql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values.Add(reader.GetValue(0).ToString() ?? "");
        return values;
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
    /// M7-DC — notification tenant isolation at the RLS runtime layer: a
    /// mention notification belongs to workspace A; the application role
    /// under a workspace-B session context cannot observe it, while a member
    /// granted workspace A can. A co-member of the same workspace who is not a recipient cannot
    /// read it either: the SELECT policy (p_app_select_recipient) binds the read to the
    /// recipient row (recipient_user_id = the caller).
    /// </summary>
    [Fact]
    public async Task NotificationItems_TenantIsolation_BCannotObserveA()
    {
        await SeedNotificationAsync(AccountA, WsA1, UserId);

        // User A is a granted member of workspace A.
        await SeedGrantAsync(AccountA, WsA1);

        // User B belongs to workspace B only — no workspace access to A: the
        // notification must be invisible.
        var userB = Guid.Parse("00000000-0000-0000-0000-00000000BB02");
        await SeedGrantAsync(AccountB, WsB1, userB);

        var seenByB = await QueryScalarsAsAppRoleAsync(
            $"SELECT title FROM notifications.notification_items WHERE source_event_id = '{NotificationSourceEventId}'",
            userId: userB);

        seenByB.Should().BeEmpty(
            "a workspace-B session must not observe workspace-A's notification");

        var seenByA = await QueryScalarsAsAppRoleAsync(
            $"SELECT title FROM notifications.notification_items WHERE source_event_id = '{NotificationSourceEventId}'",
            userId: UserId);

        seenByA.Should().BeEquivalentTo(["Mention notification A1"],
            "the recipient observes their own notification");
    }

    /// <summary>
    /// Notifications are per recipient: another member of the same workspace, with a full workspace
    /// grant, cannot read the recipient's items, their recipient rows or count them.
    /// </summary>
    [Fact]
    public async Task NotificationItems_CoMemberInTheSameWorkspace_CannotReadAnotherPersonsItems()
    {
        await SeedNotificationAsync(AccountA, WsA1, UserId);
        await SeedGrantAsync(AccountA, WsA1);

        var coMember = Guid.Parse("00000000-0000-0000-0000-00000000AA77");
        await SeedGrantAsync(AccountA, WsA1, coMember);

        var itemsSeenByCoMember = await QueryScalarsAsAppRoleAsync(
            $"SELECT title FROM notifications.notification_items WHERE source_event_id = '{NotificationSourceEventId}'",
            userId: coMember);
        var recipientsSeenByCoMember = await QueryScalarsAsAppRoleAsync(
            "SELECT recipient_user_id::text FROM notifications.notification_recipients",
            userId: coMember);
        var countSeenByCoMember = await QueryScalarsAsAppRoleAsync(
            "SELECT count(*)::text FROM notifications.notification_items",
            userId: coMember);

        itemsSeenByCoMember.Should().BeEmpty("a co-member is not the recipient");
        recipientsSeenByCoMember.Should().BeEmpty("recipient rows are the recipient's own");
        countSeenByCoMember.Should().BeEquivalentTo(["0"]);
    }

    /// <summary>
    /// The notification preference matrix is personal state: under the application role a person sees only
    /// their own set and explicit choices, never another person's in the same workspace.
    /// </summary>
    [Fact]
    public async Task NotificationPreferences_AreVisibleOnlyToTheirOwner()
    {
        var userB = Guid.Parse("00000000-0000-0000-0000-00000000BB04");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            foreach (var user in new[] { UserId, userB })
            {
                context.NotificationPreferenceSets.Add(NotificationPreferenceSet.Create(
                    AccountA, WsA1, user,
                    [new NotificationPreferenceChange(NotificationTypes.Mention, NotificationChannels.InApp, false)],
                    FixedTime)!);
            }

            await context.SaveChangesAsync();
        }

        var setsSeenByA = await QueryScalarsAsAppRoleAsync(
            "SELECT user_id::text FROM notifications.notification_preference_sets", userId: UserId);
        var entriesSeenByB = await QueryScalarsAsAppRoleAsync(
            "SELECT user_id::text FROM notifications.notification_preferences", userId: userB);
        var setsSeenByStranger = await QueryScalarsAsAppRoleAsync(
            "SELECT user_id::text FROM notifications.notification_preference_sets",
            userId: Guid.Parse("00000000-0000-0000-0000-00000000BB05"));

        setsSeenByA.Should().BeEquivalentTo([UserId.ToString()]);
        entriesSeenByB.Should().BeEquivalentTo([userB.ToString()]);
        setsSeenByStranger.Should().BeEmpty("a person never sees another person's preferences");
    }

    /// <summary>
    /// A thread mute is the muting member's own choice: under the application role a person sees only their
    /// own mutes, and a co-member of the same workspace sees none.
    /// </summary>
    [Fact]
    public async Task NotificationThreadMutes_AreVisibleOnlyToTheMutingMember()
    {
        var userB = Guid.Parse("00000000-0000-0000-0000-00000000BB06");
        var thread = Guid.Parse("A0000000-0000-0000-0000-00000000EE02");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            foreach (var user in new[] { UserId, userB })
            {
                context.NotificationThreadMutes.Add(NotificationThreadMuteRecord.Create(
                    AccountA, WsA1, user, "work-management.board-item", thread, FixedTime));
            }

            await context.SaveChangesAsync();
        }

        var seenByA = await QueryScalarsAsAppRoleAsync(
            "SELECT user_id::text FROM notifications.notification_thread_mutes", userId: UserId);
        var seenByStranger = await QueryScalarsAsAppRoleAsync(
            "SELECT user_id::text FROM notifications.notification_thread_mutes",
            userId: Guid.Parse("00000000-0000-0000-0000-00000000BB07"));

        seenByA.Should().BeEquivalentTo([UserId.ToString()]);
        seenByStranger.Should().BeEmpty("a person never sees another person's mutes");
    }

    private static readonly Guid NotificationSourceEventId =
        Guid.Parse("A0000000-0000-0000-0000-00000000EE01");

    private async Task SeedNotificationAsync(Guid accountId, Guid workspaceId, Guid recipientUserId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var notification = NotificationItemRecord.Create(
            accountId: accountId,
            workspaceId: workspaceId,
            sourceContext: "collaboration",
            notificationType: "mention.created",
            severity: NotificationSeverity.Info,
            title: "Mention notification A1",
            createdAt: FixedTime,
            actorUserId: Guid.Parse("00000000-0000-0000-0000-00000000BB03"),
            sourceEventId: NotificationSourceEventId,
            subjectType: "Mention",
            subjectId: Guid.Parse("A0000000-0000-0000-0000-00000000EE02"),
            resourceType: "work-management.board-item",
            resourceId: Guid.Parse("A0000000-0000-0000-0000-00000000AA11"),
            body: "You were mentioned in a work-management.board-item.",
            deduplicationKey: "mention-created:rls-probe");

        context.NotificationItems.Add(notification);
        context.NotificationRecipients.Add(NotificationRecipientRecord.Create(
            accountId: accountId,
            notificationId: notification.Id,
            workspaceId: workspaceId,
            recipientUserId: recipientUserId,
            createdAt: FixedTime));
        await context.SaveChangesAsync();
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

    /// <summary>
    /// Drives the production membership chain (CreateWorkspace -> AddMember ->
    /// Board) so the revocation tests prove the real handler + grant-projection
    /// path rather than a hand-seeded grant. Returns the workspace the member
    /// was added to and the member's user id.
    /// </summary>
    private async Task<(Guid WorkspaceId, Guid MemberId)> SeedWorkspaceWithMemberAndBoardAsync()
    {
        var creatorId = Guid.Parse("00000000-0000-0000-0000-000000000D01");
        var memberId = Guid.Parse("00000000-0000-0000-0000-000000000D02");

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(creatorId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(AccountA);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedTime);

        var projection = new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context));

        var createHandler = new CreateWorkspaceCommandHandler(
            context, requestContext.Object, clock.Object, projection);
        var created = await createHandler.Handle(new CreateWorkspaceCommand("Runtime Revocation Workspace", null, false), default);
        created.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        var workspaceId = created.Data;

        var addHandler = new AddMemberCommandHandler(
            context, requestContext.Object, clock.Object, projection, Mock.Of<Notrelix.Application.Features.Billing.Public.Seats.IBillingSeatGate>(g => g.CanAdmitAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)), Mock.Of<Notrelix.Application.Features.Accounts.Public.Membership.IAccountMembershipFacts>(f => f.IsActiveMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()) == Task.FromResult(true)));
        var added = await addHandler.Handle(new AddMemberCommand(workspaceId, memberId, WorkspaceRole.Member), default);
        added.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();

        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountA)
            .WithWorkspaceId(workspaceId)
            .WithCreatedBy(creatorId)
            .WithTitle("Revocation Board")
            .WithCreatedAt(FixedTime)
            .Build());
        await context.SaveChangesAsync();

        var memberGrant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == AccountA && g.WorkspaceId == workspaceId && g.UserId == memberId);
        memberGrant.MembershipStatus.Should().Be("Active");
        memberGrant.RevokedAt.Should().BeNull();

        var memberTitles = await QueryBoardTitlesAsAppRoleAsync(userId: memberId);
        memberTitles.Should().BeEquivalentTo(["Revocation Board"],
            "the member grant written by AddMember must be usable under the enforced app role");

        return (workspaceId, memberId);
    }

    /// <summary>
    /// WG-TST-MEM-INT-001 / WG-TEST-GAP-003: suspending a member through the
    /// production SuspendMember handler must revoke the workspace access grant,
    /// and the RLS predicate must deny the previously visible row immediately.
    /// </summary>
    [Fact]
    public async Task RuntimeMembershipSuspend_RevokesGrant_AndDeniesUnderAppRole()
    {
        var creatorId = Guid.Parse("00000000-0000-0000-0000-000000000D01");
        var (workspaceId, memberId) = await SeedWorkspaceWithMemberAndBoardAsync();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(creatorId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(AccountA);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedTime);

        var handler = new SuspendMemberCommandHandler(
            context, requestContext.Object, clock.Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)),
            new WorkspaceOwnerUpdateLocker(context));

        var memberVersion = await context.WorkspaceMembers.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == memberId)
            .Select(m => m.Version)
            .SingleAsync();
        var result = await handler.Handle(new SuspendMemberCommand(workspaceId, memberId, memberVersion), default);
        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();

        var member = await context.WorkspaceMembers.SingleAsync(
            m => m.WorkspaceId == workspaceId && m.UserId == memberId);
        member.Status.Should().Be(WorkspaceMemberStatus.Suspended, "the production handler must suspend the member");

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == AccountA && g.WorkspaceId == workspaceId && g.UserId == memberId);
        grant.RevokedAt.Should().NotBeNull("suspension must revoke the workspace access grant");

        var memberTitles = await QueryBoardTitlesAsAppRoleAsync(userId: memberId);
        memberTitles.Should().BeEmpty(
            "a suspended member's revoked grant must not preserve RLS visibility");
    }

    /// <summary>
    /// WG-TST-MEM-INT-001 / WG-TEST-GAP-003: removing a member through the
    /// production RemoveMember handler must revoke the workspace access grant
    /// and deny the previously visible row under the app role.
    /// </summary>
    [Fact]
    public async Task RuntimeMembershipRemoval_RevokesGrant_AndDeniesUnderAppRole()
    {
        var creatorId = Guid.Parse("00000000-0000-0000-0000-000000000D01");
        var (workspaceId, memberId) = await SeedWorkspaceWithMemberAndBoardAsync();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var requestContext = new Mock<ICurrentRequestContext>();
        requestContext.Setup(r => r.UserId).Returns(creatorId);
        requestContext.Setup(r => r.RequireAccountId()).Returns(AccountA);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(FixedTime);

        var handler = new RemoveMemberCommandHandler(
            context, requestContext.Object, clock.Object,
            new WorkspaceGrantProjectionServiceAdapter(new AccessGrantProjectionService(context)),
            new WorkspaceOwnerUpdateLocker(context));

        var memberVersion = await context.WorkspaceMembers.IgnoreQueryFilters().AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == memberId)
            .Select(m => m.Version)
            .SingleAsync();
        var result = await handler.Handle(new RemoveMemberCommand(workspaceId, memberId, memberVersion), default);
        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();

        var member = await context.WorkspaceMembers.SingleAsync(
            m => m.WorkspaceId == workspaceId && m.UserId == memberId);
        member.Status.Should().Be(WorkspaceMemberStatus.Removed, "the production handler must remove the member");

        var grant = await context.AccessGrants.SingleAsync(
            g => g.AccountId == AccountA && g.WorkspaceId == workspaceId && g.UserId == memberId);
        grant.RevokedAt.Should().NotBeNull("removal must revoke the workspace access grant");

        var memberTitles = await QueryBoardTitlesAsAppRoleAsync(userId: memberId);
        memberTitles.Should().BeEmpty(
            "a removed member's revoked grant must not preserve RLS visibility");
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

    /// <summary>
    /// WX-BE-001 / WX-BE-002 (WX-DEC-011): personal navigation rows are scoped by
    /// workspace access AND row ownership. Inside a workspace both users can
    /// access, each sees only their own recents and pins; a row in a workspace
    /// the user has no grant for stays hidden; and the app role cannot insert a
    /// row on behalf of another user (WITH CHECK).
    /// </summary>
    [Fact]
    public async Task AppRole_NavigationState_IsVisibleOnlyToItsOwnerInsideGrantedWorkspaces()
    {
        var otherUser = Guid.Parse("00000000-0000-0000-0000-0000000000BB");
        var mineRecent = Guid.Parse("A0000000-0000-0000-0000-0000000E0001");
        var theirsRecent = Guid.Parse("A0000000-0000-0000-0000-0000000E0002");
        var mineUngranted = Guid.Parse("A0000000-0000-0000-0000-0000000E0003");
        var minePin = Guid.Parse("A0000000-0000-0000-0000-0000000E0004");
        var theirsPin = Guid.Parse("A0000000-0000-0000-0000-0000000E0005");

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            context.RecentResources.AddRange(
                RecentResource.Create(AccountA, WsA1, UserId, NavigationResourceKinds.Board, mineRecent, FixedTime),
                RecentResource.Create(AccountA, WsA1, otherUser, NavigationResourceKinds.Board, theirsRecent, FixedTime),
                RecentResource.Create(AccountA, WsA2, UserId, NavigationResourceKinds.Page, mineUngranted, FixedTime));
            context.PinnedResources.AddRange(
                PinnedResource.Create(AccountA, WsA1, UserId, NavigationResourceKinds.Board, minePin, 0, FixedTime),
                PinnedResource.Create(AccountA, WsA1, otherUser, NavigationResourceKinds.Page, theirsPin, 0, FixedTime));
            await context.SaveChangesAsync();
        }

        await SeedGrantAsync(AccountA, WsA1, UserId);
        await SeedGrantAsync(AccountA, WsA1, otherUser);

        (await QueryScalarsAsAppRoleAsync("SELECT resource_id::text FROM workspace.recent_resources ORDER BY 1", userId: UserId))
            .Should().Equal([mineRecent.ToString()],
                "a member sees only their own recents, and none in a workspace without a grant");
        (await QueryScalarsAsAppRoleAsync("SELECT resource_id::text FROM workspace.recent_resources ORDER BY 1", userId: otherUser))
            .Should().Equal([theirsRecent.ToString()]);
        (await QueryScalarsAsAppRoleAsync("SELECT resource_id::text FROM workspace.pinned_resources ORDER BY 1", userId: UserId))
            .Should().Equal([minePin.ToString()]);
        (await QueryScalarsAsAppRoleAsync("SELECT resource_id::text FROM workspace.pinned_resources ORDER BY 1", userId: otherUser))
            .Should().Equal([theirsPin.ToString()]);
        (await QueryScalarsAsAppRoleAsync("SELECT resource_id::text FROM workspace.pinned_resources ORDER BY 1"))
            .Should().BeEmpty("a request without user context must see no navigation state");

        var forgedInsert = () => QueryScalarsAsAppRoleAsync($"""
            INSERT INTO workspace.pinned_resources (id, account_id, workspace_id, user_id, resource_kind, resource_id, pinned_at)
            VALUES ('{Guid.NewGuid()}', '{AccountA}', '{WsA1}', '{otherUser}', 'work-management.board', '{Guid.NewGuid()}', now())
            RETURNING id::text
            """, userId: UserId);
        (await forgedInsert.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be("42501", "the app role cannot write a pin owned by another user");
    }

    /// <summary>
    /// Onboarding state: a draft is visible only to its owner (no tenant needed); the getting-started
    /// dismissal is visible only to its owner inside a workspace the user has a grant in, and the app
    /// role cannot write a row owned by another user.
    /// </summary>
    [Fact]
    public async Task AppRole_OnboardingState_IsVisibleOnlyToItsOwner()
    {
        var otherUser = Guid.Parse("00000000-0000-0000-0000-0000000000BC");
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            context.OnboardingDrafts.AddRange(
                Notrelix.Domain.Workspaces.Onboarding.OnboardingDraft.Create(UserId, "mine", null, "{}", FixedTime),
                Notrelix.Domain.Workspaces.Onboarding.OnboardingDraft.Create(otherUser, "theirs", null, "{}", FixedTime));
            context.GettingStartedDismissals.AddRange(
                Notrelix.Domain.Workspaces.Onboarding.GettingStartedDismissal.Create(AccountA, WsA1, UserId, FixedTime),
                Notrelix.Domain.Workspaces.Onboarding.GettingStartedDismissal.Create(AccountA, WsA1, otherUser, FixedTime),
                Notrelix.Domain.Workspaces.Onboarding.GettingStartedDismissal.Create(AccountA, WsA2, UserId, FixedTime));
            await context.SaveChangesAsync();
        }

        await SeedGrantAsync(AccountA, WsA1, UserId);
        await SeedGrantAsync(AccountA, WsA1, otherUser);

        (await QueryScalarsAsAppRoleAsync("SELECT step FROM workspace.onboarding_drafts ORDER BY 1", userId: UserId))
            .Should().Equal(["mine"]);
        (await QueryScalarsAsAppRoleAsync("SELECT step FROM workspace.onboarding_drafts ORDER BY 1", userId: otherUser))
            .Should().Equal(["theirs"]);
        (await QueryScalarsAsAppRoleAsync("SELECT step FROM workspace.onboarding_drafts"))
            .Should().BeEmpty("a request without user context must see no draft");
        (await QueryScalarsAsAppRoleAsync("SELECT workspace_id::text FROM workspace.getting_started_dismissals ORDER BY 1", userId: UserId))
            .Should().Equal([WsA1.ToString()], "only the member's own dismissal in a workspace they have a grant in");

        var forgedDraft = () => QueryScalarsAsAppRoleAsync($"""
            INSERT INTO workspace.onboarding_drafts (id, user_id, step, data, created_at, updated_at, expires_at)
            VALUES ('{Guid.NewGuid()}', '{otherUser}', 'forged', '[]'::jsonb, now(), now(), now())
            RETURNING id::text
            """, userId: UserId);
        (await forgedDraft.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("42501");
    }

    /// <summary>
    /// ADR-009: Space membership entries and the Space closure index are
    /// tenant-scoped. A user with a grant only in workspace A1 sees A1's entries
    /// and closure rows, never those of a workspace without a grant, and the app
    /// role cannot write a closure row into a workspace it has no access to.
    /// </summary>
    [Fact]
    public async Task AppRole_SpaceMembersAndClosure_AreWorkspaceScoped()
    {
        var spaceA1 = Notrelix.Domain.Workspaces.Spaces.Space.Create(AccountA, WsA1, "Space A1", Notrelix.Domain.Workspaces.Spaces.SpaceVisibility.Private, UserId, FixedTime);
        var spaceB1 = Notrelix.Domain.Workspaces.Spaces.Space.Create(AccountB, WsB1, "Space B1", Notrelix.Domain.Workspaces.Spaces.SpaceVisibility.Private, UserId, FixedTime);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            context.Spaces.AddRange(spaceA1, spaceB1);
            context.SpacePaths.AddRange(
                new Notrelix.Infrastructure.Data.Workspaces.SpacePathRecord(AccountA, WsA1, spaceA1.Id, spaceA1.Id, 0),
                new Notrelix.Infrastructure.Data.Workspaces.SpacePathRecord(AccountB, WsB1, spaceB1.Id, spaceB1.Id, 0));
            await context.SaveChangesAsync();
        }

        await SeedGrantAsync(AccountA, WsA1, UserId);

        (await QueryScalarsAsAppRoleAsync("SELECT space_id::text FROM workspace.space_members ORDER BY 1", userId: UserId))
            .Should().Equal([spaceA1.Id.ToString()], "space members of a workspace without a grant stay hidden");
        (await QueryScalarsAsAppRoleAsync("SELECT descendant_id::text FROM workspace.space_paths ORDER BY 1", userId: UserId))
            .Should().Equal([spaceA1.Id.ToString()], "closure rows of a workspace without a grant stay hidden");
        (await QueryScalarsAsAppRoleAsync("SELECT descendant_id::text FROM workspace.space_paths ORDER BY 1"))
            .Should().BeEmpty("a request without user context sees no closure rows");

        var forgedInsert = () => QueryScalarsAsAppRoleAsync($"""
            INSERT INTO workspace.space_paths (ancestor_id, descendant_id, account_id, workspace_id, depth)
            VALUES ('{spaceB1.Id}', '{spaceA1.Id}', '{AccountB}', '{WsB1}', 1)
            RETURNING descendant_id::text
            """, userId: UserId);
        (await forgedInsert.Should().ThrowAsync<PostgresException>())
            .Which.SqlState.Should().Be("42501", "the app role cannot write closure rows into a workspace it cannot access");
    }

    [Fact]
    public async Task SupportReadonlyRole_ReadsNoTenantContent_AndCannotMutate()
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
            var selectAct = async () => await select.ExecuteReaderAsync();
            var selectException = await selectAct.Should().ThrowAsync<PostgresException>();
            selectException.Which.SqlState.Should().Be("42501",
                "support holds no tenant content until each case type is designed");
        }

        await using (var update = connection.CreateCommand())
        {
            update.CommandText = "UPDATE work.boards SET title = title;";
            var updateAct = async () => await update.ExecuteNonQueryAsync();
            var updateException = await updateAct.Should().ThrowAsync<PostgresException>();
            updateException.Which.SqlState.Should().Be("42501",
                "the support role has no write grant; UPDATE must be denied at the privilege level");
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO work.boards (id, account_id, workspace_id, title, board_type, board_family, item_sequence, is_archived, created_at)
            VALUES ('C0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-000000000001', 'A0000000-0000-0000-0000-00000000AA01', 'Board C1', 'Kanban', 'Standard', 0, false, '2026-06-28T00:00:00Z');
            """;
        var act = async () => await insert.ExecuteNonQueryAsync();
        var exception = await act.Should().ThrowAsync<PostgresException>();
        exception.Which.SqlState.Should().Be("42501", "the support role cannot insert");
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
    // ── D15 member profile: job title (workspace-scoped) and phone (user-owned) ─────────────

    private async Task SeedMemberJobTitlesAsync()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var memberA1 = WorkspaceMember.Create(AccountA, WsA1, Guid.Parse("00000000-0000-0000-0000-0000000000A1"), WorkspaceRole.Member, UserId, FixedTime);
        var memberA2 = WorkspaceMember.Create(AccountA, WsA2, Guid.Parse("00000000-0000-0000-0000-0000000000A2"), WorkspaceRole.Member, UserId, FixedTime);
        var memberB1 = WorkspaceMember.Create(AccountB, WsB1, Guid.Parse("00000000-0000-0000-0000-0000000000B1"), WorkspaceRole.Member, UserId, FixedTime);
        memberA1.ChangeJobTitle("Title A1", UserId, FixedTime.AddMinutes(1));
        memberA2.ChangeJobTitle("Title A2", UserId, FixedTime.AddMinutes(1));
        memberB1.ChangeJobTitle("Title B1", UserId, FixedTime.AddMinutes(1));
        context.WorkspaceMembers.AddRange(memberA1, memberA2, memberB1);
        await context.SaveChangesAsync();
    }

    private async Task<int> ExecuteAsAppRoleAsync(string sql, Guid userId)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var setup = connection.CreateCommand())
        {
            setup.Transaction = transaction;
            setup.CommandText = $"""
                SET LOCAL ROLE notrelix_app;
                SELECT set_config('app.current_user_id', '{userId}', true);
                SELECT set_config('app.request_scope', 'app', true);
                """;
            await setup.ExecuteNonQueryAsync();
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        var affected = await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return affected;
    }

    [Fact]
    public async Task WorkspaceMembers_JobTitle_IsTenantIsolated_ByWorkspaceGrant()
    {
        await SeedMemberJobTitlesAsync();
        var userA = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
        var userB = Guid.Parse("00000000-0000-0000-0000-0000000000B1");
        await SeedGrantAsync(AccountA, WsA1, userA);
        await SeedGrantAsync(AccountB, WsB1, userB);
        const string select = "SELECT job_title FROM workspace.workspace_members ORDER BY job_title";

        (await QueryScalarsAsAppRoleAsync(select, userId: UserId)).Should().BeEmpty("a user without a grant sees no member profile");
        (await QueryScalarsAsAppRoleAsync(select, userId: userA)).Should().BeEquivalentTo(["Title A1"],
            "a grant in WsA1 reaches neither WsA2 of the same account nor account B");
        (await QueryScalarsAsAppRoleAsync(select, userId: userB)).Should().BeEquivalentTo(["Title B1"]);
    }

    [Fact]
    public async Task WorkspaceMembers_JobTitle_CannotBeUpdatedAcrossWorkspaces()
    {
        await SeedMemberJobTitlesAsync();
        var userA = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
        await SeedGrantAsync(AccountA, WsA1, userA);

        var intoOtherWorkspaces = await ExecuteAsAppRoleAsync(
            $"UPDATE workspace.workspace_members SET job_title = 'Hijacked' WHERE workspace_id <> '{WsA1}'", userA);
        var intoOwnWorkspace = await ExecuteAsAppRoleAsync(
            $"UPDATE workspace.workspace_members SET job_title = 'Own' WHERE workspace_id = '{WsA1}'", userA);

        intoOtherWorkspaces.Should().Be(0, "the policy hides every other workspace's rows from the update");
        intoOwnWorkspace.Should().Be(1);
        var worker = await AppRoleLogin.CreateAsync(_container.GetConnectionString(), "rls_rt_jobtitle_worker", AppRoleLogin.WorkerRole);
        var titles = await QueryAsLoginAsync(
            worker, "SELECT job_title FROM workspace.workspace_members ORDER BY job_title", "worker");
        titles.Should().BeEquivalentTo(["Own", "Title A2", "Title B1"]);
    }

    [Fact]
    public async Task UserProfiles_Phone_IsReadableOnlyByItsOwner_AtTheDatabase()
    {
        var alice = Notrelix.Domain.Identity.Users.User.Create("alice-rls@example.com", "Alice", "hashed", FixedTime, hasPasswordCredential: true);
        var bob = Notrelix.Domain.Identity.Users.User.Create("bob-rls@example.com", "Bob", "hashed", FixedTime, hasPasswordCredential: true);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            var aliceProfile = Notrelix.Domain.Identity.Profiles.UserProfile.Create(alice.Id, FixedTime);
            aliceProfile.ApplyAccountPreferences(null, null, null, FixedTime.AddMinutes(1), "+84901234567", true);
            var bobProfile = Notrelix.Domain.Identity.Profiles.UserProfile.Create(bob.Id, FixedTime);
            bobProfile.ApplyAccountPreferences(null, null, null, FixedTime.AddMinutes(1), "+84907654321", false);
            context.Users.AddRange(alice, bob);
            context.UserProfiles.AddRange(aliceProfile, bobProfile);
            await context.SaveChangesAsync();
        }
        const string select = "SELECT phone FROM identity.user_profiles ORDER BY phone";

        (await QueryScalarsAsAppRoleAsync(select, userId: alice.Id)).Should().BeEquivalentTo(["+84901234567"],
            "the table policy is own-row only (defence in depth); sharing an opted-in phone is decided by the Workspaces member read model");
        (await QueryScalarsAsAppRoleAsync(select, userId: bob.Id)).Should().BeEquivalentTo(["+84907654321"]);
        (await QueryScalarsAsAppRoleAsync(select, userId: UserId)).Should().BeEmpty();
    }
}
