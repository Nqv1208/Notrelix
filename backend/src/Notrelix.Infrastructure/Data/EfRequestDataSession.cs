namespace Notrelix.Infrastructure.Data;

/// <summary>
/// EF Core implementation of the provider-independent data session port.
/// Owns transaction, RLS, read-only, and SaveChanges mechanics.
/// </summary>
public sealed class EfRequestDataSession : IRequestDataSession
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<EfRequestDataSession> _logger;

    public EfRequestDataSession(
        ApplicationDbContext dbContext,
        IRlsSessionContext rls,
        ILogger<EfRequestDataSession> logger)
    {
        _dbContext = dbContext;
        _rls = rls;
        _logger = logger;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        RequestDataSessionOptions options,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        if (options.Access == RequestDataAccess.None)
            return await action(cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (options.Access == RequestDataAccess.ReadOnly)
            {
                _logger.LogTrace("Setting READ ONLY transaction");
                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SET TRANSACTION READ ONLY", cancellationToken);
            }

            if (options.ApplyTenantScope)
            {
                _logger.LogTrace("Applying RLS session context");
                await _rls.ApplyAsync(cancellationToken);
            }

            var response = await action(cancellationToken);

            if (options.Access == RequestDataAccess.Transactional)
            {
                _logger.LogTrace("Saving changes");
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogTrace("Committed data session");

            return response;
        }
        catch
        {
            _logger.LogWarning("Rolling back data session");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
