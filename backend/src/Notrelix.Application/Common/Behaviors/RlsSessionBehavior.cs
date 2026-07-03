using Notrelix.Application.Common.Abstractions.Rls;

namespace Notrelix.Application.Common.Behaviors;

public class RlsSessionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _db;
    private readonly IRlsSessionContext? _rls;
    private readonly ILogger<RlsSessionBehavior<TRequest, TResponse>> _logger;

    public RlsSessionBehavior(
        IApplicationDbContext db,
        ILogger<RlsSessionBehavior<TRequest, TResponse>> logger,
        IRlsSessionContext? rls = null)
    {
        _db = db;
        _logger = logger;
        _rls = rls;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (_rls is null)
            return await next();

        _logger.LogTrace("Setting RLS session vars for {RequestType}", typeof(TRequest).Name);
        await _rls.ApplyAsync(_db.Database, ct);
        return await next();
    }
}
