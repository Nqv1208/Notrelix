using Notrelix.Application.Common.Security;

namespace Notrelix.Application.Common.Abstractions;

public interface IPermissionEvaluator
{
    Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default);
}
