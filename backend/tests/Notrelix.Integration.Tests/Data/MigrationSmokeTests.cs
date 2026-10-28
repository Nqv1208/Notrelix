using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Data;

[Collection("Database")]
[Trait("Category", "RequiresDocker")]
public class MigrationSmokeTests
{
    private readonly PostgresTestContainer _db;

    public MigrationSmokeTests(PostgresTestContainer db)
    {
        _db = db;
    }

    /// <summary>Tables a migration owns that no EF entity maps (the EF history table is excluded in the query).</summary>
    private static readonly string[] NonModelTables = [];

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_CreateExactlyTheTablesTheModelMaps()
    {
        await using var conn = _db.CreateOwnerConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT table_schema || '.' || table_name FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
              AND table_type = 'BASE TABLE'
              AND table_name <> '__EFMigrationsHistory'";

        var applied = new List<string>();
        await using (var reader = await cmd.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                applied.Add(reader.GetString(0));

        var tenant = new Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateOwnerContext(tenant);
        var mapped = context.Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null && e.GetViewName() is null)
            .Select(e => $"{e.GetSchema() ?? "public"}.{e.GetTableName()}")
            .ToHashSet();

        applied.Where(t => !mapped.Contains(t) && !NonModelTables.Contains(t)).Should().BeEmpty(
            "a table no EF entity maps must be listed in NonModelTables or removed");
        mapped.Where(t => !applied.Contains(t)).Should().BeEmpty(
            "every table the model maps must be created by the migrations");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_PublicSchemaIsEmpty()
    {
        await using var conn = _db.CreateOwnerConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'";

        var count = (long)(await cmd.ExecuteScalarAsync())!;
        count.Should().Be(0, "all v3 tables should have been dropped");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_CreatesGinTrigramIndex()
    {
        await using var conn = _db.CreateOwnerConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM pg_catalog.pg_indexes
            WHERE schemaname = 'search'
              AND indexdef ILIKE '%USING gin%'
              AND indexdef ILIKE '%search_vector%'";

        var count = (long)(await cmd.ExecuteScalarAsync())!;
        count.Should().Be(1, "GIN trigram index on search.documents.search_vector");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_CreatesOutboxTableWithRequiredColumns()
    {
        await using var conn = _db.CreateOwnerConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT column_name, data_type FROM information_schema.columns
            WHERE table_schema = 'messaging' AND table_name = 'outbox_messages'
            ORDER BY ordinal_position";

        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<(string Name, string Type)>();
        while (await reader.ReadAsync())
            columns.Add((reader.GetString(0), reader.GetString(1)));

        columns.Should().Contain(c => c.Name == "id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "event_id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "source_event_id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "source_context" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "message_name" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "schema_version" && c.Type == "integer");
        columns.Should().Contain(c => c.Name == "workspace_id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "actor_user_id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "correlation_id" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "causation_id" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "payload_json" && c.Type == "jsonb");
        columns.Should().Contain(c => c.Name == "status" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "retry_count" && c.Type == "integer");
        columns.Should().Contain(c => c.Name == "max_retries" && c.Type == "integer");
        columns.Should().Contain(c => c.Name == "next_attempt_at" && c.Type == "timestamp with time zone");
        columns.Should().Contain(c => c.Name == "processing_started_at" && c.Type == "timestamp with time zone");
        columns.Should().Contain(c => c.Name == "created_at" && c.Type == "timestamp with time zone");
        columns.Should().Contain(c => c.Name == "processed_at" && c.Type == "timestamp with time zone");
        columns.Should().Contain(c => c.Name == "error_message" && c.Type == "text");
    }
}
