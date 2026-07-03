namespace Notrelix.Application.Common.Behaviors;

public class TransactionalBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<TransactionalBehavior<TRequest, TResponse>> _logger;

    public TransactionalBehavior(
        IApplicationDbContext db,
        ILogger<TransactionalBehavior<TRequest, TResponse>> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!RequiresDbScope(request))
            return await next();

        _logger.LogTrace("Beginning transaction for {RequestType}", typeof(TRequest).Name);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();

            if (request is ITransactionalRequest or ICommand or ICommand<Unit>)
                await _db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
            _logger.LogTrace("Transaction committed for {RequestType}", typeof(TRequest).Name);

            return response;
        }
        catch
        {
            _logger.LogWarning("Transaction rolled back for {RequestType}", typeof(TRequest).Name);
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private static bool RequiresDbScope(TRequest request)
    {
        return request is ITransactionalRequest
            or IWorkspaceRequest
            or IAccountRequest
            or IRequirePermission
            or IRequireEntitlement
            or IIdempotentRequest
            || IsCacheableQuery(request);
    }

    private static bool IsCacheableQuery(TRequest request)
    {
        return request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICacheableQuery<>));
    }
}
