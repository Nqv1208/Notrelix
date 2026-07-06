namespace Notrelix.Application.Common.Security;

public interface IAuthorizationDecisionStore
{
    Task<PermissionDecision> EvaluateAsync(PermissionContext context, CancellationToken cancellationToken = default);
}
