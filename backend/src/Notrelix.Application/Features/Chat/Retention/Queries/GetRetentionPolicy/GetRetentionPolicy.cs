using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Retention.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Retention.Queries.GetRetentionPolicy;

/// <summary>
/// The Workspace's chat retention rule (UC-CHT-H05, CHT-021). Every member can read it, so people know how long their messages are kept; only
/// the Workspace Owner can change it, and the answer says which the caller is. A Workspace without a rule reads as no retention.
/// </summary>
public record GetRetentionPolicyQuery(Guid WorkspaceId)
    : IQuery<Result<RetentionPolicyDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetRetentionPolicyQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<GetRetentionPolicyQuery, Result<RetentionPolicyDto>>
{
    public async Task<Result<RetentionPolicyDto>> Handle(GetRetentionPolicyQuery request, CancellationToken cancellationToken)
    {
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, requestContext.UserId, cancellationToken);
        var canChange = string.Equals(caller.Role, "Owner", StringComparison.Ordinal);

        var policy = await db.ChatRetentionPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.WorkspaceId == request.WorkspaceId, cancellationToken);
        return Result<RetentionPolicyDto>.Success(policy is null ? RetentionPolicyDto.None(canChange) : RetentionPolicyDto.From(policy, canChange));
    }
}
