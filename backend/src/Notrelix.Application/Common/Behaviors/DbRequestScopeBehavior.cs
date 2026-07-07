using Notrelix.Application.Common.CQRS.Execution;

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
        var profile = RequestExecutionClassifier.Classify(request);

        if (profile.IsGlobal && profile.RequiresRls)
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} is global but requires tenant RLS.");
        }

        if (!profile.NeedsDbScope)
            return await next();

        _logger.LogTrace(
            "Opening DB scope for {RequestType} (write={IsTransactional}, rls={RequiresRls}, global={IsGlobal})",
            profile.RequestName,
            profile.IsTransactional,
            profile.RequiresRls,
            profile.IsGlobal);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if (profile.IsReadOnlyDbScope)
            {
                _logger.LogTrace("Setting READ ONLY for read-scope {RequestType}", profile.RequestName);
                await _db.Database.ExecuteSqlRawAsync("SET TRANSACTION READ ONLY", ct);
            }

            if (profile.RequiresRls)
            {
                _logger.LogTrace("Applying RLS session for {RequestType}", profile.RequestName);
                await _rls.ApplyAsync(_db.Database, ct);
            }

            var response = await next();

            if (profile.IsTransactional)
            {
                _logger.LogTrace("Saving changes for {RequestType}", profile.RequestName);
                await _db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            _logger.LogTrace("Committed DB scope for {RequestType}", profile.RequestName);

            return response;
        }
        catch
        {
            _logger.LogWarning("Rolling back DB scope for {RequestType}", profile.RequestName);
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
