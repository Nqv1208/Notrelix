namespace Notrelix.Application.Common.Behaviors;

public sealed class SystemOperationAuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<SystemOperationAuditBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentTenantContext _tenant;

    public SystemOperationAuditBehavior(
        ILogger<SystemOperationAuditBehavior<TRequest, TResponse>> logger,
        ICurrentTenantContext tenant)
    {
        _logger = logger;
        _tenant = tenant;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ISystemOperation systemOp)
        {
            _logger.LogWarning(
                "System operation: {Name} | {Category}: {Description} | Correlation: {Correlation} | SystemContext: {IsSystem}",
                systemOp.OperationName,
                systemOp.Reason.Category,
                systemOp.Reason.Description,
                systemOp.CorrelationId,
                _tenant.IsSystemContext);

            return await next();

            // After execution — no additional audit needed here
        }

        if (_tenant.IsSystemContext)
        {
            _logger.LogWarning(
                "System context active for non-system operation: {Request}",
                typeof(TRequest).Name);
        }

        return await next();
    }
}
