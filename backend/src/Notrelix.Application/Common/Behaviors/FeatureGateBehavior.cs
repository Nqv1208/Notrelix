namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Feature gate behavior. Runs inside DB/RLS scope, after authorization.
/// For IRequireFeature requests: checks that the specified feature is enabled
/// for the account before handler executes.
/// Features are account-scoped (V2 billing model) but may also be workspace-specific.
/// </summary>
public class FeatureGateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IFeatureGateChecker _featureGateChecker;
    private readonly IExecutionContext _executionContext;
    private readonly ILogger<FeatureGateBehavior<TRequest, TResponse>> _logger;

    public FeatureGateBehavior(
        IFeatureGateChecker featureGateChecker,
        IExecutionContext executionContext,
        ILogger<FeatureGateBehavior<TRequest, TResponse>> logger)
    {
        _featureGateChecker = featureGateChecker;
        _executionContext = executionContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IRequireFeature requireFeature)
            return await next();

        var accountId = _executionContext.AccountId;
        if (!accountId.HasValue || accountId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Feature gate skipped: no account context for {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        var featureCode = requireFeature.FeatureCode;
        var amount = requireFeature.Amount;

        var isAllowed = await _featureGateChecker.IsFeatureEnabledAsync(
            accountId.Value, featureCode, amount, cancellationToken);

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Feature gate denied: AccountId={AccountId} Feature={Feature} Amount={Amount} RequestType={RequestType}",
                accountId.Value, featureCode, amount, typeof(TRequest).Name);

            throw new ForbiddenException(
                $"Feature '{featureCode}' is not enabled for this account or usage limit reached.");
        }

        return await next();
    }
}
