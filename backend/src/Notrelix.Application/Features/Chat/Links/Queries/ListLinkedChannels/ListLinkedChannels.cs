using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Links.DTOs;
using Notrelix.Application.Features.Chat.Links.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;

namespace Notrelix.Application.Features.Chat.Links.Queries.ListLinkedChannels;

/// <summary>
/// The channels linked to a Board or a Page that the caller can see (UC-CHT-H10, the reverse lookup behind the Discuss entry point).
/// Visibility is the channel's own rule (<see cref="ChannelAccess.VisibleSpaces"/>: a public channel for a non-Guest, any other channel for
/// its active members), so a private channel the caller is not in is absent, never "hidden". A caller who cannot view the resource gets an
/// empty list too: the lookup discloses nothing about who discusses a resource they cannot open. Archived channels are listed with their
/// state. Nothing is changed.
/// </summary>
public record ListLinkedChannelsQuery(Guid WorkspaceId, string Kind, Guid ResourceId)
    : IQuery<Result<IReadOnlyList<LinkedChannelDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListLinkedChannelsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    ChatResourceViews views) : IRequestHandler<ListLinkedChannelsQuery, Result<IReadOnlyList<LinkedChannelDto>>>
{
    public async Task<Result<IReadOnlyList<LinkedChannelDto>>> Handle(ListLinkedChannelsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var title = await views.TitleAsync(requestContext.RequireAccountId(), request.WorkspaceId, userId, request.Kind, request.ResourceId, cancellationToken);
        if (title is null)
            return Result<IReadOnlyList<LinkedChannelDto>>.Success([]);

        var spaces = await ChannelAccess.VisibleSpaces(db, request.WorkspaceId, userId, ChannelAccess.IsGuest(caller))
            .Where(s => s.LinkKind == request.Kind && s.LinkId == request.ResourceId
                && (s.Kind == SpaceKind.ChannelPublic || s.Kind == SpaceKind.ChannelPrivate))
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Kind, s.ArchivedAt })
            .ToListAsync(cancellationToken);

        var ids = spaces.Select(s => s.Id).ToList();
        var mine = (await db.ChatMemberships
                .Where(m => m.UserId == userId && m.LeftAt == null && ids.Contains(m.SpaceId))
                .Select(m => m.SpaceId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        IReadOnlyList<LinkedChannelDto> result = spaces
            .Select(s => new LinkedChannelDto(s.Id, s.Name, s.Kind.ToWire(), mine.Contains(s.Id), s.ArchivedAt is not null))
            .ToList();
        return Result<IReadOnlyList<LinkedChannelDto>>.Success(result);
    }
}

public class ListLinkedChannelsQueryValidator : AbstractValidator<ListLinkedChannelsQuery>
{
    public ListLinkedChannelsQueryValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.Kind).Must(ChatResourceViews.IsLinkable)
            .WithMessage("kind must be work-management.board or documents.page.");
    }
}
