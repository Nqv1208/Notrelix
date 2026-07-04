namespace Notrelix.Application.Common.Behaviors;

/// <summary>
/// Feature gate behavior. Runs inside DB/RLS scope, after authorization.
/// For IRequireFeature requests: checks that the specified feature is enabled
/// for the workspace before handler executes.
/// </summary>
public class FeatureGateBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEntitlementChecker _checker;
    private readonly IExecutionContext _executionContext;
    private readonly ILogger<FeatureGateBehavior<TRequest, TResponse>> _logger;

    public FeatureGateBehavior(
        IEntitlementChecker checker,
        IExecutionContext executionContext,
        ILogger<FeatureGateBehavior<TRequest, TResponse>> logger)
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
        if (request is not IRequireFeature requireFeature)
            return await next();

        var workspaceId = _executionContext.WorkspaceId;
        if (!workspaceId.HasValue || workspaceId.Value == Guid.Empty)
        {
            _logger.LogWarning(
                "Feature gate skipped: no workspace context for {RequestType}",
                typeof(TRequest).Name);
            return await next();
        }

        var featureCode = requireFeature.FeatureCode;
        var amount = requireFeature.Amount;

        // Map string feature code to FeatureCode enum
        if (!Enum.TryParse<FeatureCode>(featureCode, ignoreCase: true, out var feature))
        {
            _logger.LogWarning(
                "Feature gate: unknown feature code '{FeatureCode}' for {RequestType}",
                featureCode, typeof(TRequest).Name);

            throw new ForbiddenException($"Unknown feature code: '{featureCode}'.");
        }

        var isAllowed = await _checker.CheckEntitlementAsync(
            workspaceId.Value, feature, amount, cancellationToken);

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Feature gate denied: WorkspaceId={WorkspaceId} Feature={Feature} Amount={Amount} RequestType={RequestType}",
                workspaceId.Value, featureCode, amount, typeof(TRequest).Name);

            throw new ForbiddenException(
                $"Feature '{featureCode}' is not enabled for this workspace or usage limit reached.");
        }

        return await next();
    }
}
