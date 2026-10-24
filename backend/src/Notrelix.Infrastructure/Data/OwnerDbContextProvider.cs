using Notrelix.Infrastructure.Data.Interceptors;

namespace Notrelix.Infrastructure.Data;

/// <summary>
/// Supplies the <see cref="ApplicationDbContext"/> that runs as the database owner: migrations,
/// policy application and seeding. The serving path never uses it (see the architecture test that
/// restricts its consumers).
/// </summary>
/// <remarks>
/// The owner connection string is <c>ConnectionStrings:NotrelixMigrationDb</c>. While it is unset the
/// runtime connection string <c>ConnectionStrings:NotrelixDb</c> is used for both, which keeps every
/// existing environment on its current single connection.
/// </remarks>
public sealed class OwnerDbContextProvider : IAsyncDisposable, IDisposable
{
    public const string OwnerConnectionStringName = "NotrelixMigrationDb";
    public const string RuntimeConnectionStringName = "NotrelixDb";

    private readonly IServiceProvider _services;
    private readonly ILogger<OwnerDbContextProvider> _logger;
    private readonly string? _connectionString;
    private ApplicationDbContext? _context;

    public OwnerDbContextProvider(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<OwnerDbContextProvider> logger)
    {
        _services = services;
        _logger = logger;
        _connectionString = ResolveConnectionString(configuration, out var usesRuntimeFallback);
        UsesRuntimeFallback = usesRuntimeFallback;
    }

    /// <summary>True while the owner connection falls back to the runtime connection string.</summary>
    public bool UsesRuntimeFallback { get; }

    public ApplicationDbContext Context => _context ??= Create();

    /// <summary>
    /// The owner connection string, or the runtime one when no separate owner string is configured.
    /// </summary>
    public static string? ResolveConnectionString(IConfiguration configuration, out bool usesRuntimeFallback)
    {
        var owner = configuration.GetConnectionString(OwnerConnectionStringName);
        if (!string.IsNullOrWhiteSpace(owner))
        {
            usesRuntimeFallback = false;
            return owner;
        }

        usesRuntimeFallback = true;
        return configuration.GetConnectionString(RuntimeConnectionStringName);
    }

    private ApplicationDbContext Create()
    {
        if (UsesRuntimeFallback)
        {
            _logger.LogInformation(
                "ConnectionStrings:{Owner} is not set; migrations, policy application and seeding use ConnectionStrings:{Runtime}.",
                OwnerConnectionStringName, RuntimeConnectionStringName);
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        options.AddInterceptors(_services.GetRequiredService<AuditableEntityInterceptor>());
        options.AddInterceptors(_services.GetRequiredService<DomainEventInterceptor>());
        PersistenceRegistration.ConfigureNpgsql(options, _connectionString);
        return new ApplicationDbContext(options.Options, _services.GetRequiredService<ICurrentTenantContext>());
    }

    public void Dispose() => _context?.Dispose();

    public ValueTask DisposeAsync() => _context is null ? ValueTask.CompletedTask : _context.DisposeAsync();
}
