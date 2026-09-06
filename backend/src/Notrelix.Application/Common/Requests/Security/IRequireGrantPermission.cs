using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Common.Requests.Security;

/// <summary>
/// A permission-grant mutation: the engine must additionally authorize the
/// granter's effective authority against the requested level (and any existing
/// target level) before the handler mutates Governance state. AccessFacts
/// resolves the actor's active explicit level and the target's existing level
/// in the same canonical facts query; no second authorization service is involved.
/// </summary>
public interface IRequireGrantPermission : IRequirePermission
{
    PermissionLevel RequestedLevel { get; }
}