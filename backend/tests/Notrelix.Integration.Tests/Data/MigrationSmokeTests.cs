using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Data;

[Collection("Database")]
public class MigrationSmokeTests
{
    private readonly PostgresTestContainer _db;

    public MigrationSmokeTests(PostgresTestContainer db)
    {
        _db = db;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_CreatesAll125Tables()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')";

        var count = (long)(await cmd.ExecuteScalarAsync())!;
        count.Should().Be(125);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_PublicSchemaIsEmpty()
    {
        await using var conn = _db.CreateConnection();
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
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(*) FROM pg_catalog.pg_indexes
            WHERE indexname = 'ix_search_documents_search_vector'
            AND indexdef LIKE '%gin%'";

        var count = (long)(await cmd.ExecuteScalarAsync())!;
        count.Should().Be(1, "GIN trigram index on search.documents.search_vector");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Migrations_WhenApplied_CreatesOutboxTableWithRequiredColumns()
    {
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT column_name, data_type FROM information_schema.columns
            WHERE table_schema = 'ops' AND table_name = 'outbox_messages'
            ORDER BY ordinal_position";

        await using var reader = await cmd.ExecuteReaderAsync();
        var columns = new List<(string Name, string Type)>();
        while (await reader.ReadAsync())
            columns.Add((reader.GetString(0), reader.GetString(1)));

        columns.Should().Contain(c => c.Name == "id" && c.Type == "uuid");
        columns.Should().Contain(c => c.Name == "type" && c.Type == "character varying");
        columns.Should().Contain(c => c.Name == "payload" && c.Type == "jsonb");
        columns.Should().Contain(c => c.Name == "retry_count" && c.Type == "integer");
        columns.Should().Contain(c => c.Name == "created_at" && c.Type == "timestamp with time zone");
    }
}
