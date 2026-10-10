namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// Serializes ordering mutations of one scope inside the active request
/// transaction (SPEC WM-V3-DEC-001, WMREQ051). The lock is released when that
/// transaction ends; there is no manual release. Implementations must fail when
/// no transaction is active rather than lock outside it (SPEC WMSTOP001).
/// </summary>
public interface IWorkOrderingScopeLock
{
    Task AcquireAsync(WorkOrderingScope scope, CancellationToken cancellationToken);
}
