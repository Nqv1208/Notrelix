using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Placements.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Analytics;

/// <summary>
/// TAC-AR-001 / AR-FLOW-01:E2 — real migration-chain proof for the Wave B
/// legacy tick-scale → producer-revision source_revision transition.
///
/// A scratch database is migrated only to the pre-Wave-B baseline, seeded with
/// a realistic legacy row whose source_revision is a receiver-timestamp
/// tick-scale watermark (WatermarkOf(UtcTicks), i.e. a huge value), and then
/// migrated to HEAD (the ResetLegacyPlacementSourceRevision data migration).
/// The test proves:
///   1. before the reset, a live v2 fact (producer revision 2) cannot supersede
///      the legacy tick-scale row — the exact stale-row failure mode;
///   2. the migration invalidates the row (source_revision = 0);
///   3. after the reset, the same v2 live fact applies, and the rebuild path
///      (Reconcile) never overwrites a newer live fact.
/// </summary>
[Collection("Database")]
public class LegacyPlacementSourceRevisionMigrationTests : IAsyncLifetime
{
    private const string BaselineMigrationId = "20260702093805_SchemaV2Baseline";

    private readonly PostgresTestContainer _db;
    private string _scratchDatabase = null!;

    public LegacyPlacementSourceRevisionMigrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await using var diag = CreateDiagConnection();
        await diag.OpenAsync();

        _scratchDatabase = $"notrelix_migration_transition_{Guid.NewGuid():N}";
        await using var create = diag.CreateCommand();
        create.CommandText = $"""
            CREATE DATABASE "{_scratchDatabase}"
            """;
        await create.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_scratchDatabase is not null)
        {
            await using var diag = CreateDiagConnection();
            await diag.OpenAsync();
            await using var drop = diag.CreateCommand();
            drop.CommandText = $"""
                DROP DATABASE IF EXISTS "{_scratchDatabase}" WITH (FORCE)
                """;
            await drop.ExecuteNonQueryAsync();
        }
    }

    private NpgsqlConnection CreateDiagConnection()
        => new(new NpgsqlConnectionStringBuilder(_db.ConnectionString) { Database = "postgres" }.ConnectionString);

    private string ScratchConnectionString()
        => new NpgsqlConnectionStringBuilder(_db.ConnectionString) { Database = _scratchDatabase }.ConnectionString;

    private ApplicationDbContext CreateScratchContext(ICurrentTenantContext tenant)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ScratchConnectionString(), npg =>
            {
                npg.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npg.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return new ApplicationDbContext(options, tenant);
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static WorkItemPlacementSnapshot Snapshot(
        Guid accountId, Guid itemId, Guid boardId, Guid groupId, long revision, DateTimeOffset occurredAt) =>
        new(
            AccountId: accountId,
            ItemId: itemId,
            BoardId: boardId,
            GroupId: groupId,
            IsArchived: false,
            Revision: revision,
            LastOccurredAt: occurredAt);

    [Fact]
    public async Task ResetMigration_InvalidatesLegacyTickRows_AndLiveV2FactSupersedes()
    {
        var tenant = SystemTenant();

        // 1. Scratch DB at the pre-Wave-B baseline only.
        await using (var ctx = CreateScratchContext(tenant))
        {
            await ctx.Database.MigrateAsync(BaselineMigrationId);
        }

        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var legacyGroupId = Guid.NewGuid();
        var liveGroupId = Guid.NewGuid();
        var tickScaleRevision = DateTimeOffset.UtcNow.Ticks; // WatermarkOf(UtcTicks)

        // 2. Seed a realistic legacy row (huge tick-scale source_revision).
        await using (var conn = new NpgsqlConnection(ScratchConnectionString()))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO reporting.workspace_work_item_placements
                    (id, account_id, workspace_id, item_id, board_id, group_id, is_archived, source_revision, last_occurred_at)
                VALUES
                    (@id, @accountId, @workspaceId, @itemId, @boardId, @groupId, false, @sourceRevision, @lastOccurredAt)
                """;
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("accountId", accountId);
            cmd.Parameters.AddWithValue("workspaceId", workspaceId);
            cmd.Parameters.AddWithValue("itemId", itemId);
            cmd.Parameters.AddWithValue("boardId", boardId);
            cmd.Parameters.AddWithValue("groupId", legacyGroupId);
            cmd.Parameters.AddWithValue("sourceRevision", tickScaleRevision);
            cmd.Parameters.AddWithValue("lastOccurredAt", DateTimeOffset.UtcNow);
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Before the reset, a live v2 fact (producer revision 2) is refused:
        //    the legacy tick-scale watermark masquerades as a huge revision.
        await using (var preReset = CreateScratchContext(tenant))
        {
            var service = new WorkspaceWorkItemPlacementService(preReset, new StubProjectionSource());
            var applied = await service.ApplyPlacementAsync(
                accountId, workspaceId, itemId, boardId, liveGroupId, isArchived: false,
                revision: 2, DateTimeOffset.UtcNow, CancellationToken.None);
            applied.Should().BeFalse(
                "a legacy tick-scale source_revision must not be compared directly to a small producer revision");
            await preReset.SaveChangesAsync();
            preReset.ChangeTracker.Clear();

            var row = await preReset.WorkspaceWorkItemPlacements
                .SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
            row.GroupId.Should().Be(legacyGroupId);
            row.SourceRevision.Should().Be(tickScaleRevision);
        }

        // 4. Apply the new data migration to HEAD → legacy rows invalidated.
        await using (var migrated = CreateScratchContext(tenant))
        {
            await migrated.Database.MigrateAsync();
        }

        await using (var postReset = CreateScratchContext(tenant))
        {
            var row = await postReset.WorkspaceWorkItemPlacements
                .SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
            row.SourceRevision.Should().Be(0,
                "the reset migration invalidates legacy rows so producer revisions can supersede them");

            // 5. The same live v2 fact now applies.
            var service = new WorkspaceWorkItemPlacementService(postReset, new StubProjectionSource());
            var applied = await service.ApplyPlacementAsync(
                accountId, workspaceId, itemId, boardId, liveGroupId, isArchived: false,
                revision: 2, DateTimeOffset.UtcNow, CancellationToken.None);
            applied.Should().BeTrue("after invalidation the producer revision is strictly newer than 0");
            await postReset.SaveChangesAsync();
            postReset.ChangeTracker.Clear();

            var updated = await postReset.WorkspaceWorkItemPlacements
                .SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
            updated.GroupId.Should().Be(liveGroupId);
            updated.SourceRevision.Should().Be(2);

            // 6. Rebuild reconciliation: an older rebuild snapshot (revision 1)
            //    must not overwrite the newer live fact (revision 2).
            var rebuildService = new WorkspaceWorkItemPlacementService(postReset, new StubProjectionSource());
            await rebuildService.RebuildWorkspaceAsync(workspaceId, [
                Snapshot(accountId, itemId, boardId, legacyGroupId, revision: 1, DateTimeOffset.UtcNow),
            ], CancellationToken.None);
            await postReset.SaveChangesAsync();
            postReset.ChangeTracker.Clear();

            var reconciled = await postReset.WorkspaceWorkItemPlacements
                .SingleAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId);
            reconciled.GroupId.Should().Be(liveGroupId,
                "an authoritative rebuild must never overwrite a newer live fact");
            reconciled.SourceRevision.Should().Be(2);
        }
    }

    private sealed class StubProjectionSource : IWorkItemProjectionSourceAdapter
    {
        public Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
            Guid workspaceId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<WorkItemPlacementSnapshot>>([]);

        public Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
            Guid workspaceId, Guid itemId, CancellationToken cancellationToken)
            => Task.FromResult<WorkItemPlacementSnapshot?>(null);
    }
}