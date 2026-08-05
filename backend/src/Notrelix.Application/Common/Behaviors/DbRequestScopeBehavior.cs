using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Behaviors;

public class DbRequestScopeBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IRequestDataSession _dataSession;
    private readonly ILogger<DbRequestScopeBehavior<TRequest, TResponse>> _logger;

    public DbRequestScopeBehavior(
        IRequestDataSession dataSession,
        ILogger<DbRequestScopeBehavior<TRequest, TResponse>> logger)
    {
        _dataSession = dataSession;
        _logger = logger;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var profile = RequestExecutionClassifier.Classify(request);

        if (profile.IsGlobal && profile.RequiresRls)
        {
            throw new SecurityMisconfigurationException(
                $"{profile.RequestName} is global but requires tenant RLS.");
        }

        if (!profile.NeedsDbScope)
            return next();

        _logger.LogTrace(
            "Opening DB scope for {RequestType} (write={IsTransactional}, rls={RequiresRls}, global={IsGlobal})",
            profile.RequestName,
            profile.IsTransactional,
            profile.RequiresRls,
            profile.IsGlobal);

        var options = new RequestDataSessionOptions(
            Access: profile.IsTransactional
                ? RequestDataAccess.Transactional
                : RequestDataAccess.ReadOnly,
            ApplyTenantScope: profile.IsTenantScoped,
            ApplyResourceScope: profile.IsResourceScoped);

        return _dataSession.ExecuteAsync(options, _ => next(), ct);
    }
}
