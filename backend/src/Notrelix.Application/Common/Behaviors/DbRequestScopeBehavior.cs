namespace Notrelix.Application.Common.Behaviors;

public class DbRequestScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _db;
    private readonly IRlsSessionContext _rls;
    private readonly ILogger<DbRequestScopeBehavior<TRequest, TResponse>> _logger;

    public DbRequestScopeBehavior(
        IApplicationDbContext db,
        IRlsSessionContext rls,
        ILogger<DbRequestScopeBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _rls = rls;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var isWrite = request is ITransactionalRequest;
        var requiresRls = request is IRlsReadRequest
            or IRequirePermission
            or IRequireSubscription
            or IRequireFeature;

        if (!isWrite && !requiresRls)
            return await next();

        _logger.LogTrace("Opening DB/RLS scope for {RequestType} (write={IsWrite})", typeof(TRequest).Name, isWrite);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _logger.LogTrace("Applying RLS session for {RequestType}", typeof(TRequest).Name);
            await _rls.ApplyAsync(_db.Database, ct);

            var response = await next();

            if (isWrite)
            {
                _logger.LogTrace("Saving changes for {RequestType}", typeof(TRequest).Name);
                await _db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogTrace("Committed DB/RLS scope for {RequestType}", typeof(TRequest).Name);

            return response;
        }
        catch
        {
            _logger.LogWarning("Rolling back DB/RLS scope for {RequestType}", typeof(TRequest).Name);
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
