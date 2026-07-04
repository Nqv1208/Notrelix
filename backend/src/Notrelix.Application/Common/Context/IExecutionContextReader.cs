namespace Notrelix.Application.Common.Context;

public interface IExecutionContextReader
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Name { get; }
    bool IsAuthenticated { get; }
    Guid? AccountId { get; }
    Guid? WorkspaceId { get; }
    bool IsSystemContext { get; }
    Guid CorrelationId { get; }
    Guid? CausationId { get; }
    bool IsResolved { get; }

    Guid RequireUserId();
    Guid RequireAccountId();
    Guid RequireWorkspaceId();
}
