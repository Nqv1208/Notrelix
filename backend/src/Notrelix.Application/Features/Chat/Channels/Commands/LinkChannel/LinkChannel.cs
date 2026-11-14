using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Channels.DTOs;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;

namespace Notrelix.Application.Features.Chat.Channels.Commands.LinkChannel;

/// <summary>
/// Links a channel to a Board or a Page by ResourceRef (UC-CHT-H10, CHT-023). Needs a channel manager, a Workspace Owner or Admin who can
/// also view the resource now (403 otherwise, the same answer for a hidden, trashed or missing resource). The resource is never changed:
/// the link is stored on the channel only. A channel has one link; a second one replaces it. A stale expectedVersion is 412. A Team is
/// not linkable yet (Governance publishes no Team visibility fact).
/// </summary>
public record LinkChannelCommand(Guid WorkspaceId, Guid ChannelId, string Kind, Guid ResourceId, long ExpectedVersion)
    : ICommand<Result<ChannelDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.ChatChannelManage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("chat.channel"), ChannelId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class LinkChannelCommandHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock,
    ChatResourceViews views,
    ChatAuditFacts audit) : IRequestHandler<LinkChannelCommand, Result<ChannelDto>>
{
    public async Task<Result<ChannelDto>> Handle(LinkChannelCommand request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var accountId = requestContext.RequireAccountId();
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadChannelAsync(db, request.WorkspaceId, request.ChannelId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);
        ChannelAccess.RequireManager(space, caller, userId);
        if (ChannelAccess.IsGuest(caller))
            throw new ForbiddenException("A Guest cannot link a channel to a resource.");

        // The manager must be able to view the resource now; the answer is the same for hidden, trashed and missing.
        var title = await views.TitleAsync(accountId, request.WorkspaceId, userId, request.Kind, request.ResourceId, cancellationToken);
        if (title is null)
            throw new ForbiddenException("You cannot view this resource, so you cannot link a channel to it.");

        var (beforeKind, beforeId) = (space.LinkKind, space.LinkId);
        if (space.SetLink(request.Kind, request.ResourceId, userId, clock.UtcNow))
            await audit.ChannelLinkChangedAsync(space, beforeKind, beforeId, space.LinkKind, space.LinkId, cancellationToken);

        var dto = ChannelDto.From(space, space.GetActiveMembership(userId), caller.Role);
        return Result<ChannelDto>.Success(dto with { Link = new(request.Kind, request.ResourceId, true, title) });
    }
}

public class LinkChannelCommandValidator : AbstractValidator<LinkChannelCommand>
{
    public LinkChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Kind).Must(ChatResourceViews.IsLinkable)
            .WithMessage("kind must be work-management.board or documents.page; a Team cannot be linked yet.");
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
