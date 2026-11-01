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

    private string RawConnectionString =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER"))
            ? $"{_container.GetConnectionString()};Include Error Detail=true"
            : throw new InvalidOperationException(
                "Docker is not available (NO_DOCKER env var is set). Use --filter \"Category!=RequiresDocker\" to skip Docker-dependent tests.");

    /// <summary>
    /// The container superuser: the owner of every table. It migrates, applies the policy pack, seeds and
    /// resets, and verifies outcomes. It bypasses row-level security, so no code under test may run on it.
    /// </summary>
    public string OwnerConnectionString => RawConnectionString;

    /// <summary>
    /// The connection the serving process runs on: a non-owner login running as <c>notrelix_app</c>, so row-level
    /// security applies. Hosts, pipelines and handlers under test take this string.
    /// </summary>
    public string ConnectionString => AppConnectionString;

    /// <summary>The application-role login (same as <see cref="ConnectionString"/>).</summary>
    public string AppConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// The login background work (dispatchers, deduplication, consumers, sweeps) runs on: a non-owner login
    /// running as <c>notrelix_worker</c>, so the worker policies apply.
    /// </summary>
    public string WorkerConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// The platform operator console's login: the real <c>notrelix_operator</c> LOGIN role, provisioned the way
    /// <c>--rls-apply</c> provisions it, so <c>session_user</c> is the operator role itself.
    /// </summary>
    public string OperatorConnectionString { get; private set; } = string.Empty;

    /// <summary>
    /// Applies the row-level-security policy pack (roles, helpers, policies, owner-defined functions) as the
    /// owner. The fixture applies it once after migrating, because adapters call pack functions (for
    /// example the co-member profile resolver); the owner connection bypasses the policies themselves.
    /// </summary>
    public async Task ApplyRlsAsync()
    {
        await using var context = CreateOwnerContext(new FakeCurrentTenantContext());
        await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
    }

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
        {
            await _container.StartAsync();

            var tenant = new FakeCurrentTenantContext();
            tenant.SetWorkspace(Guid.Parse("00000000-0000-0000-0000-000000000001"), Guid.Parse("00000000-0000-0000-0000-000000000001"), null);
            await using var context = CreateOwnerContext(tenant);
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
            AppConnectionString = await AppRoleLogin.CreateAsync(OwnerConnectionString, "notrelix_test_app_login");
            WorkerConnectionString = await AppRoleLogin.CreateAsync(
                OwnerConnectionString, "notrelix_test_worker_login", AppRoleLogin.WorkerRole);

            var operatorSecret = Guid.NewGuid().ToString("N");
            await using (var provisioning = new NpgsqlConnection(OwnerConnectionString))
            {
                await RlsLoginRoleProvisioner.ProvisionAsync(
                    provisioning, [new LoginRoleCredential(RlsLoginRoleProvisioner.OperatorRoleName, operatorSecret)],
                    NullLogger.Instance);
            }

            OperatorConnectionString = new NpgsqlConnectionStringBuilder(OwnerConnectionString)
            {
                Username = RlsLoginRoleProvisioner.OperatorRoleName,
                Password = operatorSecret,
            }.ConnectionString;
        }
    }

    public async Task DisposeAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_DOCKER")))
            return;
        await _container.DisposeAsync();
    }

    /// <summary>
    /// A context on the owner connection, for seeding and verification only (row-level security does not
    /// apply). Code under test takes <see cref="ConnectionString"/> or <see cref="WorkerConnectionString"/>.
    /// </summary>
    public ApplicationDbContext CreateOwnerContext(ICurrentTenantContext? tenant = null, params IInterceptor[] interceptors) =>
        CreateContextFor(OwnerConnectionString, tenant, interceptors);

    /// <summary>A context on the application-role login (row-level security applies).</summary>
    public ApplicationDbContext CreateAppContext(ICurrentTenantContext? tenant = null, params IInterceptor[] interceptors) =>
        CreateContextFor(AppConnectionString, tenant, interceptors);

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

    /// <summary>An owner connection, for seeding and verification only.</summary>
    public NpgsqlConnection CreateOwnerConnection()
    {
        return new NpgsqlConnection(OwnerConnectionString);
    }
}
