using Notrelix.Infrastructure.Data.Rls;
namespace Notrelix.Infrastructure.Data;

/// <summary>Which database login a dependency-injection scope's <see cref="ApplicationDbContext"/> uses.</summary>
public enum DatabasePrincipalKind
{
    /// <summary>The non-owner application login (<c>notrelix_app</c>): request-serving work, bounded by row-level security.</summary>
    Application = 0,

    /// <summary>
    /// The background login (<c>notrelix_worker</c>): outbox and email dispatchers, the deduplication
    /// store, message consumers, sweeps and reapers. The only principal the unbounded worker scope
    /// (<c>ops.is_worker_scope()</c>) is honoured for.
    /// </summary>
    Worker = 1,
}

/// <summary>
/// Connection strings of the serving process. <c>ConnectionStrings:NotrelixDb</c> is the application login;
/// <c>ConnectionStrings:NotrelixWorkerDb</c> is the background login. It is required while
/// <c>Rls:RoleEnforcement</c> is <c>Enforce</c> (the default of every environment); only a host that opted out
/// of enforcement (<c>Warn</c> or <c>Off</c>: the test hosts and the OpenAPI export) falls back to the
/// application string.
/// </summary>
public sealed class DatabaseConnectionStrings
{
    public const string WorkerConnectionStringName = "NotrelixWorkerDb";
    public const string ApplicationConnectionStringName = "NotrelixDb";

    public DatabaseConnectionStrings(string? application, string? worker, bool workerUsesApplicationFallback)
    {
        Application = application;
        Worker = worker;
        WorkerUsesApplicationFallback = workerUsesApplicationFallback;
    }

    public string? Application { get; }

    public string? Worker { get; }

    /// <summary>True while the background login falls back to the application connection string.</summary>
    public bool WorkerUsesApplicationFallback { get; }

    public static DatabaseConnectionStrings FromConfiguration(IConfiguration configuration)
    {
        var application = configuration.GetConnectionString(ApplicationConnectionStringName);
        var worker = configuration.GetConnectionString(WorkerConnectionStringName);
        if (!string.IsNullOrWhiteSpace(worker))
        {
            return new DatabaseConnectionStrings(application, worker, false);
        }

        if (!string.IsNullOrWhiteSpace(application) && RlsRoleEnforcement.IsEnforced(configuration))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{WorkerConnectionStringName} is required: background work connects as the " +
                "notrelix_worker login and never as the application login. Only Rls:RoleEnforcement=Warn|Off hosts " +
                "may omit it.");
        }

        return new DatabaseConnectionStrings(application, application, true);
    }
}

/// <summary>
/// The principal of one dependency-injection scope. It is chosen before the scope's first database use
/// (a background loop creates its scope with <see cref="WorkerScopeExtensions.CreateWorkerAsyncScope"/>; a
/// message-consume scope is switched by the first consume filter) and cannot change afterwards: the
/// context's connection is fixed when it is first resolved.
/// </summary>
public sealed class DatabasePrincipal
{
    private readonly DatabaseConnectionStrings _connectionStrings;
    private bool _connectionResolved;

    public DatabasePrincipal(DatabaseConnectionStrings connectionStrings)
    {
        _connectionStrings = connectionStrings;
    }

    public DatabasePrincipalKind Kind { get; private set; } = DatabasePrincipalKind.Application;

    public bool IsWorker => Kind == DatabasePrincipalKind.Worker;

    /// <summary>Switches this scope to the background login. Fails when the connection was already resolved as another principal.</summary>
    public void UseWorker()
    {
        if (Kind == DatabasePrincipalKind.Worker)
            return;

        if (_connectionResolved)
        {
            throw new InvalidOperationException(
                "The scope's database connection was already resolved as the application login; " +
                "choose the worker principal before the first database use.");
        }

        Kind = DatabasePrincipalKind.Worker;
    }

    /// <summary>Resolves (and from then on fixes) the connection string of this scope's principal.</summary>
    internal string? ResolveConnectionString()
    {
        _connectionResolved = true;
        return IsWorker ? _connectionStrings.Worker : _connectionStrings.Application;
    }
}

public static class WorkerScopeExtensions
{
    /// <summary>
    /// Creates a dependency-injection scope whose <see cref="ApplicationDbContext"/> connects as the
    /// background login. Every background loop (dispatchers, sweeps, reapers) creates its scopes this way.
    /// </summary>
    public static AsyncServiceScope CreateWorkerAsyncScope(this IServiceScopeFactory scopeFactory)
    {
        var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<DatabasePrincipal>().UseWorker();
        return scope;
    }
}
