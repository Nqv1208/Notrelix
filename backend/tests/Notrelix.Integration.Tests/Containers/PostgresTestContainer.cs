using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Infrastructure.Data;

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
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
            return;
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString, npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .Options;
        return new ApplicationDbContext(options);
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(ConnectionString);
    }
}
