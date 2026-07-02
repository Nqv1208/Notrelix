using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure.Data;

/// <summary>
/// Composite implementation of <see cref="IApplicationDbContext"/> that delegates
/// SaveChangesAsync to all 4 split DbContexts within a single transaction.
/// </summary>
internal sealed class TransactionDbContext : IApplicationDbContext
{
    private readonly DbContext[] _contexts;

    public TransactionDbContext(
        DbContext platform,
        DbContext product,
        DbContext projection,
        DbContext infrastructure)
    {
        _contexts = [platform, product, projection, infrastructure];
    }

    /// <summary>
    /// Database facade delegates to the first (PlatformDbContext) context.
    /// All contexts share the same underlying NpgsqlConnection and transaction.
    /// </summary>
    public DatabaseFacade Database => _contexts[0].Database;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var ctx in _contexts)
        {
            total += await ctx.SaveChangesAsync(cancellationToken);
        }
        return total;
    }
}
