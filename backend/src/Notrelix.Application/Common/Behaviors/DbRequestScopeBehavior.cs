namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Single DB/RLS/Transaction boundary.
/// Replaces the old RlsSessionBehavior + TransactionalBehavior combination.
///
/// For requests requiring DB scope (ITransactionalRequest, IWorkspaceRequest, IRlsReadRequest, etc.):
///   1. Begin transaction
///   2. Apply RLS session variables (SET LOCAL)
///   3. Execute inner behaviors (Authorization, Idempotency, Entitlement, Handler)
///   4. SaveChanges (for commands)
///   5. Commit transaction
///
/// For requests not requiring DB scope: pass through.
/// </summary>
public class DbRequestScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _db;
    private readonly IRlsSessionContext? _rls;
    private readonly ILogger<DbRequestScopeBehavior<TRequest, TResponse>> _logger;

    public DbRequestScopeBehavior(
        IApplicationDbContext db,
        ILogger<DbRequestScopeBehavior<TRequest, TResponse>> logger,
        IRlsSessionContext? rls = null)
    {
        _db = db;
        _logger = logger;
        _rls = rls;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!RequiresDbScope(request))
            return await next();

        _logger.LogTrace("Opening DB/RLS scope for {RequestType}", typeof(TRequest).Name);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Apply RLS session variables inside transaction
            if (_rls is not null)
            {
                _logger.LogTrace("Applying RLS session for {RequestType}", typeof(TRequest).Name);
                await _rls.ApplyAsync(_db.Database, ct);
            }

            // Execute inner behaviors and handler
            var response = await next();

            // SaveChanges for commands
            if (request is ITransactionalRequest or ICommand or ICommand<Unit>)
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

    private static bool RequiresDbScope(TRequest request)
    {
        return request is ITransactionalRequest
            or IWorkspaceRequest
            or IAccountRequest
            or IRequirePermission
            or IIdempotentRequest
            or IRlsReadRequest
            || IsCacheableQuery(request)
            || IsAuthorizedCacheableRequest(request);
    }

    private static bool IsCacheableQuery(TRequest request)
    {
        return request.GetType().GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICacheableQuery<>));
    }

    private static bool IsAuthorizedCacheableRequest(TRequest request)
    {
        return request is IAuthorizedCacheableRequest;
    }
}
