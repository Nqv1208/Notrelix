using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// Guards the existing-database upgrade path: a database that already carries the row-level-security policy
/// pack must still apply every later migration. The shared fixture migrates first and applies the pack second,
/// so it never meets this path; a migration that alters the type of a policy-referenced column (EF
/// <c>AlterColumn</c> emits <c>ALTER COLUMN ... TYPE</c>) fails here with 0A000.
/// </summary>
public sealed class MigrationsOnRlsDatabaseTests
{
    // The policy pack targets the current schema, so the start point must be recent enough to have the columns it reads.
    private const string BeforeBoardTemplateScope = "20261027150000_BackfillDefaultBoardGroups";

    [Fact]
    public async Task MigrationsAfterTheStartPoint_ApplyOnADatabaseThatAlreadyHasRlsPolicies()
    {
        await using var container = new PostgreSqlBuilder()
            .WithImage("postgres:16.15-bookworm")
            .WithDatabase("notrelix_migrate_on_rls")
            .WithUsername("notrelix")
            .WithPassword("notrelix_test")
            .WithCleanUp(true)
            .Build();
        await container.StartAsync();

        await using var context = CreateContext(container.GetConnectionString());
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(BeforeBoardTemplateScope);
        await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();

        var upgrade = () => migrator.MigrateAsync();
        await upgrade.Should().NotThrowAsync();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        await using var connection = new NpgsqlConnection(container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema = 'work' AND table_name = 'board_templates'
              AND column_name IN ('account_id', 'workspace_id') AND is_nullable = 'NO'
            """, connection);
        Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(2);
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
