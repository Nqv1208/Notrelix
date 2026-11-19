using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Features.Chat.Channels.Queries.ListDeletedChannels;

/// <summary>A deleted channel the caller may restore, with the last day it can be.</summary>
public sealed record DeletedChannelDto(Guid Id, string Name, string Kind, DateTimeOffset DeletedAt, Guid? DeletedBy, DateTimeOffset RestorableUntil, long Version);

/// <summary>
/// The deleted channels the caller could restore (UC-CHT-A08): inside the 30-day window, and passing the same visibility and
/// manager rules as the delete itself, so a private channel's name never reaches someone who could not manage it. Newest delete first.
/// </summary>
public record ListDeletedChannelsQuery(Guid WorkspaceId)
    : IQuery<Result<IReadOnlyList<DeletedChannelDto>>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListDeletedChannelsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members,
    IDateTimeProvider clock) : IRequestHandler<ListDeletedChannelsQuery, Result<IReadOnlyList<DeletedChannelDto>>>
{
    public async Task<Result<IReadOnlyList<DeletedChannelDto>>> Handle(ListDeletedChannelsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var now = clock.UtcNow;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        if (ChannelAccess.IsGuest(caller))
            return Result<IReadOnlyList<DeletedChannelDto>>.Success([]);

        var floor = now - Space.RestoreWindow;
        // Bypassing the context's soft-delete filter also drops its tenant predicate (EF Core 9): the account and the Workspace are applied here.
        var accountId = requestContext.RequireAccountId();
        var deleted = await db.ChatSpaces.IgnoreQueryFilters()
            .Where(s => s.AccountId == accountId && s.WorkspaceId == request.WorkspaceId && s.DeletedAt != null && s.DeletedAt >= floor
                && (s.Kind == SpaceKind.ChannelPublic || s.Kind == SpaceKind.ChannelPrivate))
            .ToListAsync(cancellationToken);

        var ids = deleted.Select(s => s.Id).ToArray();
        await db.ChatMemberships.Where(m => ids.Contains(m.SpaceId)).ToListAsync(cancellationToken);

        IReadOnlyList<DeletedChannelDto> page = deleted
            .Where(s => ChannelAccess.CanSee(s, caller, userId) && ChannelAccess.CanManage(s, caller, userId))
            .OrderByDescending(s => s.DeletedAt)
            .Select(s => new DeletedChannelDto(
                s.Id, s.Name, s.Kind.ToWire(), s.DeletedAt!.Value, s.DeletedBy, s.DeletedAt.Value + Space.RestoreWindow, s.Version))
            .ToList();
        return Result<IReadOnlyList<DeletedChannelDto>>.Success(page);
    }
}
