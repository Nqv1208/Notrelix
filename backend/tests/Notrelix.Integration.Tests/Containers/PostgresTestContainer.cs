using Microsoft.EntityFrameworkCore.Diagnostics;
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
            ? $"{_container.GetConnectionString()};Include Error Detail=true"
            : throw new InvalidOperationException(
                "Docker is not available (NO_DOCKER env var is set). Use --filter \"Category!=RequiresDocker\" to skip Docker-dependent tests.");

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
        {
            await _container.StartAsync();

            // Use EnsureCreated so the schema matches the current model.
            // MigrateAsync applies the last snapshot migration, which may be
            // out of sync with the current model (pending model changes cause
            // PendingModelChangesWarning and inconsistent schema).
            //
            // PendingModelChangesWarning is suppressed just like production:
            // see PersistenceRegistration.cs which also ignores it.
            var workspace = new FakeCurrentWorkspace();
            workspace.SetWorkspace(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            await using var context = CreateContext(workspace);
            await context.Database.EnsureCreatedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
            return;
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateContext(ICurrentWorkspace? currentWorkspace = null, params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .UseSnakeCaseNamingConvention()
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var options = optionsBuilder.Options;
        return currentWorkspace is not null
            ? new ApplicationDbContext(options, currentWorkspace)
            : new ApplicationDbContext(options);
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}
