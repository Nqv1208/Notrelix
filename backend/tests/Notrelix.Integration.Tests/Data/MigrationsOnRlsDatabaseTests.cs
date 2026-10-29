using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Data;

/// <summary>
/// Guards the existing-database upgrade path: a database that already carries the row-level-security policy
/// pack must still apply every migration after the anchor. The shared fixture migrates first and applies the pack
/// second, so it never meets this path; a migration that alters the type of a policy-referenced column (EF
/// <c>AlterColumn</c> emits <c>ALTER COLUMN ... TYPE</c>) fails here with 0A000.
/// </summary>
[Trait("Category", "RequiresDocker")]
public sealed class MigrationsOnRlsDatabaseTests
{
    // The policy pack targets the current schema, so the anchor must be recent enough to have the columns it reads.
    // When the pack outgrows the anchor, the policy apply step fails: move the anchor to a newer migration.
    private const string Anchor = "20261028180000_ScopeBoardTemplatesToWorkspace";

    [Fact]
    public async Task EveryMigrationAfterTheAnchor_AppliesOnADatabaseThatAlreadyHasRlsPolicies()
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
        await migrator.MigrateAsync(Anchor);
        await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();

        var upgrade = () => migrator.MigrateAsync();
        await upgrade.Should().NotThrowAsync();

        (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
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
