using Microsoft.EntityFrameworkCore.Storage;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.CrossContext.Collaboration.Notifications;
using Notrelix.Infrastructure.Data.Notifications;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Tables that carry no account or workspace column of their own inherit the scope of the row they belong
/// to (or of their owner). Rows are seeded as the owner; the application login then proves what it can read
/// and write inside a scope it holds and nothing outside it.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DerivedScopePolicyTests : IAsyncLifetime
{
    private const string PostgresRlsViolation = "42501";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public DerivedScopePolicyTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "derived-a@example.com", "DerivedA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "derived-b@example.com", "DerivedB", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string WorkspaceScope(AppRoleTenant t) => $"account:{t.AccountId:N}:workspace:{t.WorkspaceId:N}";
    private static string UserScope(AppRoleTenant t, Guid userId) => $"account:{t.AccountId:N}:user:{userId:N}";

    private async Task OwnerExecAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Runs <paramref name="work"/> on the application login inside the tenant scope of <paramref name="tenant"/>.</summary>
    private async Task<T> AsAppAsync<T>(AppRoleTenant tenant, Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
            scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        return await work(connection, transaction);
    }

    private static async Task<long> CountAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var command = new NpgsqlCommand(sql, c, t);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> InsertIsDeniedAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var savepoint = new NpgsqlCommand("SAVEPOINT attempt", c, t);
        await savepoint.ExecuteNonQueryAsync();
        try
        {
            await using var command = new NpgsqlCommand(sql, c, t);
            await command.ExecuteNonQueryAsync();
            return false;
        }
        catch (PostgresException e) when (e.SqlState == PostgresRlsViolation)
        {
            await using var rollback = new NpgsqlCommand("ROLLBACK TO SAVEPOINT attempt", c, t);
            await rollback.ExecuteNonQueryAsync();
            return true;
        }
    }

    private static string IdempotencyInsert(string scope, string key) =>
        "INSERT INTO ops.idempotency_records (id, scope, operation, key_hash, request_hash, state, created_at, expires_at) " +
        $"VALUES (gen_random_uuid(), '{scope}', 'op', '{key}', 'r', 'Started', now(), now() + interval '1 hour')";

    [Fact]
    public async Task IdempotencyRecords_AreVisibleAndWritableOnlyInsideAScopeTheCallerHolds()
    {
        await OwnerExecAsync(IdempotencyInsert(WorkspaceScope(_b), "owner-b"));
        await OwnerExecAsync(IdempotencyInsert("system", "owner-system"));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(WorkspaceScope(_a), "a-workspace"))).Should().BeFalse("the caller's own workspace partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert($"account:{_a.AccountId:N}", "a-account"))).Should().BeFalse("the caller's own account partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(UserScope(_a, _a.User.Id), "a-user"))).Should().BeFalse("the caller's own user partition");

            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(WorkspaceScope(_b), "a-into-b"))).Should().BeTrue("another tenant's partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(UserScope(_a, _b.User.Id), "a-as-b"))).Should().BeTrue("another person's user partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert("system", "a-system"))).Should().BeTrue("the system partition is the worker's");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert("whatever", "a-free-text"))).Should().BeTrue("a partition that is not tenant-qualified");

            (await CountAsync(c, t, "SELECT count(*) FROM ops.idempotency_records WHERE key_hash IN ('owner-b', 'owner-system')"))
                .Should().Be(0, "neither another tenant's record nor the system record is readable");
            (await CountAsync(c, t, "SELECT count(*) FROM ops.idempotency_records WHERE key_hash LIKE 'a-%'"))
                .Should().Be(3);
            return 0;
        });
    }

    [Fact]
    public async Task MfaRecoveryCodes_AreTheOwnersOwn_ThroughTheirBatch()
    {
        var batchA = Guid.NewGuid();
        var batchB = Guid.NewGuid();
        await OwnerExecAsync(
            "INSERT INTO identity.mfa_recovery_batches (id, created_at, user_id, version) VALUES (@a, now(), @ua, 1), (@b, now(), @ub, 1);" +
            "INSERT INTO identity.mfa_recovery_codes (id, batch_id, code_hash) VALUES (gen_random_uuid(), @a, 'ha'), (gen_random_uuid(), @b, 'hb')",
            ("a", batchA), ("b", batchB), ("ua", _a.User.Id), ("ub", _b.User.Id));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_batches")).Should().Be(1);
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_codes WHERE code_hash = 'ha'")).Should().Be(1);
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_codes WHERE code_hash = 'hb'"))
                .Should().Be(0, "another person's recovery code is invisible");
            (await InsertIsDeniedAsync(c, t,
                $"INSERT INTO identity.mfa_recovery_codes (id, batch_id, code_hash) VALUES (gen_random_uuid(), '{batchB}', 'x')"))
                .Should().BeTrue("a code cannot be added to another person's batch");
            return 0;
        });
    }

    [Fact]
    public async Task NotificationRecipients_AreTheRecipientsOwn()
    {
        await OwnerExecAsync(
            "SET session_replication_role = replica;" +
            "INSERT INTO notifications.notification_recipients (id, account_id, workspace_id, notification_id, recipient_user_id, status, delivery_policy_json, created_at) VALUES " +
            "(gen_random_uuid(), @aa, @aw, gen_random_uuid(), @ua, 'Unread', '{}', now()), " +
            "(gen_random_uuid(), @ba, @bw, gen_random_uuid(), @ub, 'Unread', '{}', now())",
            ("aa", _a.AccountId), ("aw", _a.WorkspaceId), ("ua", _a.User.Id),
            ("ba", _b.AccountId), ("bw", _b.WorkspaceId), ("ub", _b.User.Id));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_recipients"))
                .Should().Be(1, "a person reads only their own recipient rows");
            return 0;
        });
    }

    [Fact]
    public async Task InboxMarkRead_ChangesOnlyTheCallersOwnRecipientRows_NeverAnotherRecipients()
    {
        var coMember = await AppRoleMembers.AddAsync(
            _fixture, _a, "InboxPeer", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        var ownersNotification = Guid.NewGuid();
        var peersNotification = Guid.NewGuid();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            foreach (var (recipient, title) in new[] { (_a.User.Id, "inbox-owner"), (coMember.Id, "inbox-peer") })
            {
                var item = NotificationItemRecord.Create(
                    accountId: _a.AccountId, workspaceId: _a.WorkspaceId, sourceContext: "collaboration",
                    notificationType: "mention.created", severity: NotificationSeverity.Info, title: title,
                    createdAt: DateTimeOffset.UtcNow, deduplicationKey: $"dedup-{title}");
                ctx.NotificationItems.Add(item);
                ctx.NotificationRecipients.Add(NotificationRecipientRecord.Create(
                    _a.AccountId, item.Id, _a.WorkspaceId, recipient, DateTimeOffset.UtcNow));
                if (recipient == _a.User.Id) ownersNotification = item.Id; else peersNotification = item.Id;
            }

            await ctx.SaveChangesAsync();
        }

        async Task<T> InboxAsAsync<T>(AppRoleTenant who, Func<PostgresNotificationInbox, Task<T>> act)
        {
            await using var db = _fixture.CreateAppContext(new FakeCurrentTenantContext());
            await using var tx = await db.Database.BeginTransactionAsync();
            await using (var scope = new NpgsqlCommand(
                "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
                "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
                (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)tx.GetDbTransaction()))
            {
                scope.Parameters.AddWithValue("u", who.User.Id.ToString());
                scope.Parameters.AddWithValue("a", who.AccountId.ToString());
                scope.Parameters.AddWithValue("w", who.WorkspaceId.ToString());
                await scope.ExecuteNonQueryAsync();
            }

            var result = await act(new PostgresNotificationInbox(db, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow)));
            await tx.CommitAsync();
            return result;
        }

        async Task<long> UnreadOfAsync(Guid recipient)
        {
            await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT count(*) FROM notifications.notification_recipients WHERE recipient_user_id = @u AND read_at IS NULL", connection);
            command.Parameters.AddWithValue("u", recipient);
            return (long)(await command.ExecuteScalarAsync())!;
        }

        (await InboxAsAsync(_a, i => i.MarkReadAsync(_a.WorkspaceId, _a.User.Id, peersNotification, DateTimeOffset.UtcNow, default)))
            .Should().BeFalse("a notification addressed to someone else is not the caller's to mark");
        (await InboxAsAsync(_a, i => i.MarkReadAsync(_a.WorkspaceId, coMember.Id, peersNotification, DateTimeOffset.UtcNow, default)))
            .Should().BeFalse("naming another recipient does not widen the row policy");
        (await UnreadOfAsync(coMember.Id)).Should().Be(1, "the peer's notification is still unread");

        (await InboxAsAsync(_a, i => i.MarkAllReadAsync(_a.WorkspaceId, coMember.Id, DateTimeOffset.UtcNow, default)))
            .Should().Be(0, "read-all as the owner cannot reach the peer's rows even when the peer is named");
        (await UnreadOfAsync(coMember.Id)).Should().Be(1);

        (await InboxAsAsync(_a, i => i.MarkReadAsync(_a.WorkspaceId, _a.User.Id, ownersNotification, DateTimeOffset.UtcNow, default)))
            .Should().BeTrue();
        (await UnreadOfAsync(_a.User.Id)).Should().Be(0);
        (await UnreadOfAsync(coMember.Id)).Should().Be(1, "the owner's own mark leaves the peer untouched");

        var peerTenant = _a with { User = coMember };
        (await InboxAsAsync(peerTenant, i => i.MarkAllReadAsync(_a.WorkspaceId, coMember.Id, DateTimeOffset.UtcNow, default)))
            .Should().Be(1);
        (await UnreadOfAsync(coMember.Id)).Should().Be(0);
    }

    [Fact]
    public async Task NotificationItems_AreReadableOnlyByTheirRecipient_NotByACoMemberOfTheSameWorkspace()
    {
        var coMember = await AppRoleMembers.AddAsync(
            _fixture, _a, "CoMember", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");

        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            foreach (var (recipient, title) in new[] { (_a.User.Id, "for-owner"), (coMember.Id, "for-comember") })
            {
                var item = NotificationItemRecord.Create(
                    accountId: _a.AccountId, workspaceId: _a.WorkspaceId, sourceContext: "collaboration",
                    notificationType: "mention.created", severity: NotificationSeverity.Info, title: title,
                    createdAt: DateTimeOffset.UtcNow, deduplicationKey: $"dedup-{title}");
                ctx.NotificationItems.Add(item);
                ctx.NotificationRecipients.Add(NotificationRecipientRecord.Create(
                    _a.AccountId, item.Id, _a.WorkspaceId, recipient, DateTimeOffset.UtcNow));
            }

            await ctx.SaveChangesAsync();
        }

        await AsAppAsync(_a, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_items"))
                .Should().Be(1, "the owner of the workspace reads only the item addressed to them");
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_items WHERE title = 'for-comember'"))
                .Should().Be(0, "another member's item is invisible even to the workspace owner");
            return 0;
        });

        var coMemberTenant = _a with { User = coMember };
        await AsAppAsync(coMemberTenant, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_items WHERE title = 'for-owner'"))
                .Should().Be(0, "a co-member cannot read the owner's notification items");
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_items WHERE title = 'for-comember'"))
                .Should().Be(1);
            return 0;
        });

        await using var worker = new NpgsqlConnection(_fixture.WorkerConnectionString);
        await worker.OpenAsync();
        await using var count = new NpgsqlCommand("SELECT count(*) FROM notifications.notification_items", worker);
        ((long)(await count.ExecuteScalarAsync())!).Should().Be(2, "background readers run on the worker login and read every item");
    }

    [Fact]
    public async Task IntegrationSecretBlobs_CanBeStagedInsideATenantScope_AndAreUnreadableUnlessAConnectionOfTheScopeReferencesThem()
    {
        var blob = Guid.NewGuid();
        await AsAppAsync(_a, async (c, t) =>
        {
            (await InsertIsDeniedAsync(c, t,
                $"INSERT INTO integration.integration_secret_blobs (id, created_at, encrypted_payload, revoked) VALUES ('{blob}', now(), 'x', false)"))
                .Should().BeFalse("a blob is staged inside the caller's own tenant scope");
            (await CountAsync(c, t, "SELECT count(*) FROM integration.integration_secret_blobs"))
                .Should().Be(0, "an unreferenced blob is not readable, even by the scope that staged it");
            await t.CommitAsync();
            return 0;
        });

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        (await InsertIsDeniedAsync(connection, transaction,
            $"INSERT INTO integration.integration_secret_blobs (id, created_at, encrypted_payload, revoked) VALUES ('{Guid.NewGuid()}', now(), 'x', false)"))
            .Should().BeTrue("without a scope the application login cannot stage a blob");
    }
}
