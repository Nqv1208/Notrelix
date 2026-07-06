namespace Notrelix.Application.Common.Security;

public interface IPermissionEvaluator
{
    Task<PermissionDecision> EvaluateAsync(
        PermissionContext context,
        CancellationToken cancellationToken = default);
}
