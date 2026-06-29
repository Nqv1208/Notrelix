using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Containers;

public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("notrelix_test")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public string ConnectionString =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER"))
            ? _container.GetConnectionString()
            : throw new InvalidOperationException(
                "Docker is not available (NO_DOCKER env var is set). Use --filter \"Category!=RequiresDocker\" to skip Docker-dependent tests.");

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
        {
            await _container.StartAsync();

            // Migrations must build the EF Core model with a workspace context that
            // produces a real workspace filter (not `false` and not empty/no-op).
            // Using a dedicated migration workspace ID ensures the model has a
            // proper e.WorkspaceId == @ws filter expression. Test contexts with
            // different workspace IDs get their own cached model via the custom
            // IModelCacheKeyFactory, and EF Core re-evaluates the filter with
            // the test context's _currentWorkspace at query time.
            var workspace = new FakeCurrentWorkspace();
            workspace.SetWorkspace(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            await using var context = CreateContext(workspace);
            await context.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
            return;
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateContext(ICurrentWorkspace? currentWorkspace = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;
        return currentWorkspace is not null
            ? new ApplicationDbContext(options, currentWorkspace)
            : new ApplicationDbContext(options);
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}
