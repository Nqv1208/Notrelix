namespace Notrelix.Application.Common.Context;

public interface ICurrentRequestContext
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
    bool IsSystemContext { get; }

    Guid RequireAccountId();
    Guid RequireWorkspaceId();
}
