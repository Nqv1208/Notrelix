namespace Notrelix.Application.Common.Context;

public interface ICurrentUser
{
    Guid UserId { get; }
    string Email { get; }
    string Name { get; }
    bool IsAuthenticated { get; }
    Guid? WorkspaceId { get; }
    Guid? SessionId { get; }
}
