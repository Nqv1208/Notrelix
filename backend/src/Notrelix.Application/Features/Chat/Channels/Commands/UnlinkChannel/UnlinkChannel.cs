using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Channels.Commands.UnlinkChannel;

/// <summary>
/// Removes a channel's link (UC-CHT-H10). Needs a channel manager, a Workspace Owner or Admin; the resource is not touched and need not be
/// viewable (a manager can always remove a link that became hidden). A channel with no link is a no-op that answers the channel as it is.
/// A stale expectedVersion is 412.
/// </summary>
public record UnlinkChannelCommand(Guid WorkspaceId, Guid ChannelId, long ExpectedVersion)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UnlinkChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatAuditFacts audit) : IRequestHandler<UnlinkChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(UnlinkChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);

        var (beforeKind, beforeId) = (space.LinkKind, space.LinkId);
        if (space.ClearLink(userId, clock.UtcNow))
            await audit.ChannelLinkChangedAsync(space, beforeKind, beforeId, null, null, cancellationToken);

        return Result<ChannelDto>.Success(ChannelDto.From(space, space.GetActiveMembership(userId), caller.Role));
    }
}

public class UnlinkChannelCommandValidator : AbstractValidator<UnlinkChannelCommand>
{
    public UnlinkChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
