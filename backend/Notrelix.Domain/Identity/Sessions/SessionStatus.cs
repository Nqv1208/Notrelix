using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions;

public enum SessionStatus
{
    Active,
    Revoked,
    Expired
}
