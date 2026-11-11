using Notrelix.Application.Common.Models;

namespace Notrelix.Application.Features.Chat.Mentions.Queries.CheckBroadcastAccess;

/// <summary>
/// The broadcast action of the Workspace as a request, so the access policy has one contract to evaluate it with (UC-CHT-G03).
/// The pipeline answers it: a caller without chat.mention.broadcast is refused (403); a caller with it gets true. A send and the
/// mention picker evaluate the same contract through <c>IBroadcastAuthorizer</c>.
/// </summary>
public record CheckBroadcastAccessQuery(Guid WorkspaceId)
    : IQuery<Result<bool>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ChatMentionBroadcast;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class CheckBroadcastAccessQueryHandler : IRequestHandler<CheckBroadcastAccessQuery, Result<bool>>
{
    public Task<Result<bool>> Handle(CheckBroadcastAccessQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<bool>.Success(true));
}
