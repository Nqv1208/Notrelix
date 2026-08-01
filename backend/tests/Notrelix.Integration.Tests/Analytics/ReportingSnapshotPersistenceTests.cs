using Npgsql;
using Notrelix.Domain.Analytics.Snapshots;
using Notrelix.Domain.SharedKernel;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Analytics;

[Collection("Database")]
public class ReportingSnapshotPersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public ReportingSnapshotPersistenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SchemaV2Snapshot_RoundTrips_RetainsVersion()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _db.CreateContext(tenant);

        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var payload = ReportSnapshotPayload.Create("BoardSummary", 2, JsonValue.Create("""{"total":42}"""));
        var snapshot = ReportingSnapshot.Capture(accountId, workspaceId, payload, DateTimeOffset.UtcNow);

        ctx.ReportingSnapshots.Add(snapshot);
        await ctx.SaveChangesAsync();

        await using var fresh = _db.CreateContext(tenant);
        var loaded = await fresh.ReportingSnapshots
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == snapshot.Id);

        loaded.SchemaVersion.Should().Be(2);
        loaded.ReportType.Should().Be("BoardSummary");
        loaded.Payload.Should().Be(payload);
    }

    [Fact]
    public async Task SchemaV1Snapshot_RoundTrips_AsVersion1()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _db.CreateContext(tenant);

        var payload = ReportSnapshotPayload.CreateV1("BoardVelocity", JsonValue.Create("""{"total":100}"""));
        var snapshot = ReportingSnapshot.Capture(Guid.NewGuid(), Guid.NewGuid(), payload, DateTimeOffset.UtcNow);

        ctx.ReportingSnapshots.Add(snapshot);
        await ctx.SaveChangesAsync();

        await using var fresh = _db.CreateContext(tenant);
        var loaded = await fresh.ReportingSnapshots
            .IgnoreQueryFilters()
            .FirstAsync(s => s.Id == snapshot.Id);

        loaded.SchemaVersion.Should().Be(1);
        loaded.Payload.Should().Be(payload);
    }

    [Fact]
    public async Task LegacyRowWithoutSchemaVersion_LoadsAsVersion1()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _db.CreateContext(tenant);

        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var data = """{"total":7}""";
        var capturedAt = DateTimeOffset.UtcNow;

        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO reporting.reporting_snapshots (id, account_id, workspace_id, report_type, data, captured_at)
            VALUES (@id, @accountId, @workspaceId, @reportType, @data::jsonb, @capturedAt)
            """;
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("accountId", accountId);
        cmd.Parameters.AddWithValue("workspaceId", workspaceId);
        cmd.Parameters.AddWithValue("reportType", "BoardSummary");
        cmd.Parameters.AddWithValue("data", data);
        cmd.Parameters.AddWithValue("capturedAt", capturedAt);
        await cmd.ExecuteNonQueryAsync();

        await using var fresh = _db.CreateContext(tenant);
        var loaded = await fresh.ReportingSnapshots
            .IgnoreQueryFilters()
            .FirstAsync(s => s.WorkspaceId == workspaceId);

        loaded.SchemaVersion.Should().Be(1);
        var expected = ReportSnapshotPayload.CreateV1("BoardSummary", JsonValue.Create(data));
        loaded.Payload.Should().Be(expected);
    }
}
