namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Subscription gate behavior. Runs inside DB/RLS scope, after authorization.
/// For IRequireSubscription requests: checks that workspace has an active subscription
/// (and optionally meets a minimum tier) before handler executes.
/// </summary>
public class SubscriptionGateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEntitlementChecker _checker;
    private readonly IExecutionContext _executionContext;
    private readonly ILogger<SubscriptionGateBehavior<TRequest, TResponse>> _logger;

    public SubscriptionGateBehavior(
        IEntitlementChecker checker,
        IExecutionContext executionContext,
        ILogger<SubscriptionGateBehavior<TRequest, TResponse>> logger)
    {
        _checker = checker;
        _executionContext = executionContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireSubscription requireSubscription)
            return await next();

        var workspaceId = _executionContext.WorkspaceId;
        if (!workspaceId.HasValue || workspaceId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Subscription gate skipped: no workspace context for {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        var minimumTier = requireSubscription.MinimumTier;

        bool hasAccess;
        if (!string.IsNullOrEmpty(minimumTier))
        {
            hasAccess = await _checker.HasSubscriptionTierAsync(
                workspaceId.Value, minimumTier, cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "Subscription gate denied: WorkspaceId={WorkspaceId} RequiredTier={Tier} RequestType={RequestType}",
                    workspaceId.Value, minimumTier, typeof(TRequest).Name);

                throw new ForbiddenException(
                    $"This feature requires at least the '{minimumTier}' subscription tier.");
            }
        }
        else
        {
            hasAccess = await _checker.HasActiveSubscriptionAsync(
                workspaceId.Value, cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "Subscription gate denied: WorkspaceId={WorkspaceId} NoActiveSubscription RequestType={RequestType}",
                    workspaceId.Value, typeof(TRequest).Name);

                throw new ForbiddenException(
                    "This feature requires an active subscription.");
            }
        }

        return await next();
    }
}
