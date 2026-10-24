using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Containers;

public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
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

    /// <summary>The container superuser: the owner of every table, used to migrate, seed and apply policies.</summary>
    public string OwnerConnectionString => ConnectionString;

    /// <summary>
    /// Applies the row-level-security policy pack (roles, helpers, policies, owner-defined functions) as the
    /// owner. The fixture applies it once after migrating, because adapters call pack functions (for
    /// example the co-member profile resolver); the owner connection bypasses the policies themselves.
    /// </summary>
    public async Task ApplyRlsAsync()
    {
        await using var context = CreateContext(new FakeCurrentTenantContext());
        await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
        {
            await _container.StartAsync();

            var tenant = new FakeCurrentTenantContext();
            tenant.SetWorkspace(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000001"), null);
            await using var context = CreateContext(tenant);
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
            return;
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null, params IInterceptor[] interceptors) =>
        CreateContextFor(ConnectionString, tenant, interceptors);

    /// <summary>A context over an explicit connection string (for example the application-role login).</summary>
    public static ApplicationDbContext CreateContextFor(
        string connectionString, ICurrentTenantContext? tenant = null, params IInterceptor[] interceptors)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>();

        if (interceptors.Length > 0)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        var options = optionsBuilder.Options;
        return tenant is not null
            ? new ApplicationDbContext(options, tenant)
            : new ApplicationDbContext(options);
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}
