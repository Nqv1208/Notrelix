namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// A permission-grant mutation: the engine must additionally authorize the
/// granter's effective authority rank against the requested rank (and any
/// existing target rank) before the handler mutates Governance state. AccessFacts
/// resolves the actor's active explicit rank and the target's existing rank in
/// the same canonical facts query; no second authorization service is involved.
/// Ranks are technical ordering values owned by the Governance vocabulary; the
/// pipeline seam only compares integers.
/// </summary>
public interface IRequireGrantPermission : IRequirePermission
{
    int RequestedPermissionRank { get; }
}