using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Integration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// The migration that makes <c>idx_account_slug</c> unique must run on a database that already holds Accounts
/// with the same slug (every personal Account used to be named from the display name alone): the oldest keeps
/// its slug, each later one gets a tail from its own id, and only then is the unique index built.
/// </summary>
[Trait("Category", "RequiresDocker")]
public sealed class AccountSlugMigrationTests
{
    private const string BeforeTheMigration = "20261121061838_AddSsoIdentities";

    [Fact]
    public async Task DuplicateSlugs_AreRenamedExceptTheOldest_ThenTheIndexIsUnique()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16.15-bookworm")
            .WithDatabase("notrelix_slug_migration")
            .WithUsername("notrelix")
            .WithPassword("notrelix_test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();
        var connectionString = container.GetConnectionString();

        await using var context = CreateContext(connectionString);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(BeforeTheMigration);

        var oldest = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var middle = Guid.Parse("00000000-0000-0000-0000-0000000000a2");
        var newest = Guid.Parse("00000000-0000-0000-0000-0000000000a3");
        var unique = Guid.Parse("00000000-0000-0000-0000-0000000000a4");
        await InsertAccountAsync(connectionString, middle, "s-account", "2026-03-02T00:00:00Z");
        await InsertAccountAsync(connectionString, oldest, "s-account", "2026-03-01T00:00:00Z");
        await InsertAccountAsync(connectionString, newest, "s-account", "2026-03-03T00:00:00Z");
        await InsertAccountAsync(connectionString, unique, "acme", "2026-03-01T00:00:00Z");

        await migrator.MigrateAsync();

        var slugs = await ReadSlugsAsync(connectionString);
        slugs[oldest].Should().Be("s-account", "the oldest Account keeps its slug");
        slugs[unique].Should().Be("acme", "an Account whose slug was never shared is untouched");
        slugs[middle].Should().Be("s-account-" + middle.ToString("N"));
        slugs[newest].Should().Be("s-account-" + newest.ToString("N"));
        slugs.Values.Distinct().Should().HaveCount(slugs.Count);

        var duplicate = () => InsertAccountAsync(connectionString, Guid.NewGuid(), "acme", "2026-03-04T00:00:00Z");
        var failure = await duplicate.Should().ThrowAsync<PostgresException>();
        failure.Which.SqlState.Should().Be("23505");
        failure.Which.ConstraintName.Should().Be("idx_account_slug");
    }

    private static async Task InsertAccountAsync(string connectionString, Guid id, string slug, string createdAt)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO account.accounts (id, name, slug, status, type, created_at, updated_at) " +
            "VALUES (@id, 'x', @slug, 'Active', 'Personal', @at, @at)", connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("slug", slug);
        command.Parameters.AddWithValue("at", DateTimeOffset.Parse(createdAt));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<Dictionary<Guid, string>> ReadSlugsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT id, slug FROM account.accounts", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var slugs = new Dictionary<Guid, string>();
        while (await reader.ReadAsync())
            slugs[reader.GetGuid(0)] = reader.GetString(1);
        return slugs;
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return new ApplicationDbContext(options);
    }
}
