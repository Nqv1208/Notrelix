namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Subscription gate behavior. Runs inside DB/RLS scope, after authorization.
/// For IRequireSubscription requests: checks that the account has an active subscription
/// (and optionally meets a minimum tier) before handler executes.
/// Subscriptions are account-scoped, not workspace-scoped (V2 billing model).
/// </summary>
public class SubscriptionGateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISubscriptionChecker _subscriptionChecker;
    private readonly IExecutionContextReader _executionContext;
    private readonly ILogger<SubscriptionGateBehavior<TRequest, TResponse>> _logger;

    public SubscriptionGateBehavior(
        ISubscriptionChecker subscriptionChecker,
        IExecutionContextReader executionContext,
        ILogger<SubscriptionGateBehavior<TRequest, TResponse>> logger)
    {
        _subscriptionChecker = subscriptionChecker;
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

        var accountId = _executionContext.AccountId;
        if (!accountId.HasValue || accountId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Subscription gate failed: no account context for {RequestType}",
                typeof(TRequest).Name);
            throw new SecurityMisconfigurationException(
                $"Subscription gate failed: no account context for {typeof(TRequest).Name}");
        }

        var minimumTier = requireSubscription.MinimumTier;

        bool hasAccess;
        if (!string.IsNullOrEmpty(minimumTier))
        {
            hasAccess = await _subscriptionChecker.HasMinimumTierAsync(
                accountId.Value, minimumTier, cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "Subscription gate denied: AccountId={AccountId} RequiredTier={Tier} RequestType={RequestType}",
                    accountId.Value, minimumTier, typeof(TRequest).Name);

                throw new ForbiddenException(
                    $"This feature requires at least the '{minimumTier}' subscription tier.");
            }
        }
        else
        {
            hasAccess = await _subscriptionChecker.HasActiveSubscriptionAsync(
                accountId.Value, cancellationToken);

            if (!hasAccess)
            {
                _logger.LogWarning(
                    "Subscription gate denied: AccountId={AccountId} NoActiveSubscription RequestType={RequestType}",
                    accountId.Value, typeof(TRequest).Name);

                throw new ForbiddenException(
                    "This feature requires an active subscription.");
            }
        }

        return await next();
    }
}
