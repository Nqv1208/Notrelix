namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// A permission revoke mutation: the engine must enforce that the actor's
/// effective authority is not lower than the target permission's existing level.
/// AccessFacts resolves the target level in the same canonical facts query.
/// </summary>
public interface IRequireRevokePermission : IRequirePermission
{
    Guid TargetPermissionId { get; }
}