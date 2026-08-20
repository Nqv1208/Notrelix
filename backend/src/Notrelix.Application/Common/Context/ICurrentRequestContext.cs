namespace Notrelix.Application.Common.Context;

public interface ICurrentRequestContext
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
    bool IsSystemContext { get; }

    /// <summary>Session id bound into the access token (sid claim). Null for tokens issued before session binding or non-session principals.</summary>
    Guid? SessionId { get; }

    Guid RequireAccountId();
    Guid RequireWorkspaceId();
}
