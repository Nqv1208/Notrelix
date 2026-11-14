using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Permissions.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Permissions.Queries.GetChannelPermissions;

/// <summary>
/// The overrides of a channel and its version (UC-CHT-H01). Anyone who can see the channel reads them, so a member can see why a control is
/// off. A channel the caller may not see is the same NotFound as a missing one.
/// </summary>
public record GetChannelPermissionsQuery(Guid WorkspaceId, Guid ChannelId)
    : IQuery<Result<ChannelPermissionsDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetChannelPermissionsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<GetChannelPermissionsQuery, Result<ChannelPermissionsDto>>
{
    public async Task<Result<ChannelPermissionsDto>> Handle(GetChannelPermissionsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        return Result<ChannelPermissionsDto>.Success(ChannelPermissionsDto.From(space));
    }
}

public class GetChannelPermissionsQueryValidator : AbstractValidator<GetChannelPermissionsQuery>
{
    public GetChannelPermissionsQueryValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
    }
}
