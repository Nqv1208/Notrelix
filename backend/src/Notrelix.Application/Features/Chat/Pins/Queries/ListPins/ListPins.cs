using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Pins.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Pins.Queries.ListPins;

/// <summary>
/// The pinned messages of a space, newest pin first (UC-CHT-F07). Any caller who can see the space reads them; a private
/// space the caller is not a member of, or a Guest outside the spaces they were added to, is the same NotFound as a missing
/// one. The cursor is the nextCursor of the previous page (the pin time and id of its last item); the page size is 1 to 100.
/// </summary>
public record ListPinsQuery(Guid WorkspaceId, Guid SpaceId, string? Cursor, int Limit)
    : IQuery<Result<PinPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The page size when the caller names none (UC-CHT-F07).</summary>
    public const int DefaultLimit = ChatLimits.PageSizeDefault;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListPinsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListPinsQuery, Result<PinPageDto>>
{
    public async Task<Result<PinPageDto>> Handle(ListPinsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var query = db.ChatPins.AsNoTracking().Where(p => p.SpaceId == space.Id);
        if (request.Cursor is not null)
        {
            var (pinnedAt, id) = ParseCursor(request.Cursor);
            query = query.Where(p => p.PinnedAt < pinnedAt || (p.PinnedAt == pinnedAt && p.Id < id));
        }

        var rows = await query
            .OrderByDescending(p => p.PinnedAt)
            .ThenByDescending(p => p.Id)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        var page = rows.Take(request.Limit).ToList();

        var messageIds = page.Select(p => p.MessageId).ToList();
        var messages = await db.ChatMessages.AsNoTracking()
            .Where(m => m.SpaceId == space.Id && messageIds.Contains(m.Id))
            .ToListAsync(cancellationToken);
        var pinnedMessages = messages.ToDictionary(m => m.Id);
        var ordered = page.Where(p => pinnedMessages.ContainsKey(p.MessageId)).Select(p => pinnedMessages[p.MessageId]).ToList();
        var dtos = await ReplyPreviews.DtosAsync(db, request.WorkspaceId, ordered, cancellationToken);
        var dtoById = dtos.ToDictionary(d => d.Id);

        var items = page
            .Where(p => dtoById.ContainsKey(p.MessageId))
            .Select(p => new PinDto(dtoById[p.MessageId], p.PinnedBy, p.PinnedAt))
            .ToList();

        var next = rows.Count > request.Limit && page.Count > 0
            ? CursorOf(page[^1])
            : null;
        return Result<PinPageDto>.Success(new PinPageDto(items, next));
    }

    private static string CursorOf(Pin pin) => $"{pin.PinnedAt.UtcTicks}.{pin.Id:N}";

    private static (DateTimeOffset PinnedAt, Guid Id) ParseCursor(string cursor)
    {
        var parts = cursor.Split('.');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var ticks) || !Guid.TryParseExact(parts[1], "N", out var id))
            throw new Notrelix.Application.Common.Exceptions.ValidationException("cursor is not valid.");
        return (new DateTimeOffset(ticks, TimeSpan.Zero), id);
    }
}

public class ListPinsQueryValidator : AbstractValidator<ListPinsQuery>
{
    public ListPinsQueryValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.PageSizeMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.PageSizeMax}.");
    }
}
