using Notrelix.Application.Common.SystemOperations;

namespace Notrelix.Infrastructure.Data;

public sealed class SystemContextScope : IDisposable
{
    private readonly ICurrentTenantContext _tenant;
    private readonly ILogger _logger;
    private readonly string _operationName;
    private bool _disposed;

    public SystemContextScope(
        ICurrentTenantContext tenant,
        ILogger logger,
        string operationName,
        SystemOperationReason reason)
    {
        _tenant = tenant;
        _logger = logger;
        _operationName = operationName;

        _logger.LogWarning(
            "System context scope started: {Operation} | {Category}: {Description}",
            operationName, reason.Category, reason.Description);

        _tenant.SetSystem();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _tenant.Clear();
            _disposed = true;

            _logger.LogWarning(
                "System context scope ended: {Operation}",
                _operationName);
        }
    }
}
