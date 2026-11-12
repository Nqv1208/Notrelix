using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;

/// <summary>
/// Who reacted to a message, earliest reaction first (UC-CHT-G07). Only a reader of the space sees the reactors: a private space the
/// caller is not a member of, or a Guest outside the spaces they were added to, is the same NotFound as a missing message. An
/// optional kind narrows the list to one emoji. The cursor is the nextCursor of the previous page (the reaction time and id of its
/// last item); the page size is 1 to 100.
/// </summary>
public record ListReactorsQuery(Guid WorkspaceId, Guid MessageId, string? Kind, string? Cursor, int Limit)
    : IQuery<Result<ReactorPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The page size when the caller names none.</summary>
    public const int DefaultLimit = ChatLimits.PageSizeDefault;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListReactorsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListReactorsQuery, Result<ReactorPageDto>>
{
    public async Task<Result<ReactorPageDto>> Handle(ListReactorsQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);

        var message = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(
            m => m.Id == request.MessageId && m.WorkspaceId == request.WorkspaceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Message), request.MessageId);

        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, message.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var query = db.ChatReactions.AsNoTracking().Where(r => r.MessageId == message.Id);
        if (request.Kind is not null)
            query = query.Where(r => r.Kind == request.Kind);
        if (request.Cursor is not null)
        {
            var (reactedAt, id) = ParseCursor(request.Cursor);
            query = query.Where(r => r.CreatedAt > reactedAt || (r.CreatedAt == reactedAt && r.Id > id));
        }

        var rows = await query
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);
        var page = rows.Take(request.Limit).ToList();

        var items = page.Select(r => new ReactorDto(r.Kind, r.UserId, r.CreatedAt)).ToList();
        var next = rows.Count > request.Limit && page.Count > 0
            ? $"{page[^1].CreatedAt.UtcTicks}.{page[^1].Id:N}"
            : null;
        return Result<ReactorPageDto>.Success(new ReactorPageDto(items, next));
    }

    private static (DateTimeOffset ReactedAt, Guid Id) ParseCursor(string cursor)
    {
        var parts = cursor.Split('.');
        if (parts.Length != 2 || !long.TryParse(parts[0], out var ticks) || !Guid.TryParseExact(parts[1], "N", out var id))
            throw new Notrelix.Application.Common.Exceptions.ValidationException("cursor is not valid.");
        return (new DateTimeOffset(ticks, TimeSpan.Zero), id);
    }
}

public class ListReactorsQueryValidator : AbstractValidator<ListReactorsQuery>
{
    public ListReactorsQueryValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Kind)
            .Must(ReactionKinds.Contains)
            .When(x => x.Kind is not null)
            .WithMessage("The emoji is not in the reaction palette.");
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.PageSizeMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.PageSizeMax}.");
    }
}
