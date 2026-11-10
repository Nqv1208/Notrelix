using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Services;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;

/// <summary>
/// A page of a space's messages in sequence order (UC-CHT-B02, B03). Exactly one cursor may be given: before (older
/// than a sequence), after (newer than a sequence) or around (a window around a sequence). With none, the newest
/// page. Tombstones are included. The caller must be able to see the space; otherwise it is the same NotFound as a
/// missing space.
/// </summary>
public record ListMessagesQuery(Guid WorkspaceId, Guid SpaceId, long? Before, long? After, long? Around, int Limit)
    : IQuery<Result<MessagePageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    /// <summary>The page size when the caller names none (spec UC-CHT-B02).</summary>
    public const int DefaultLimit = ChatLimits.PageSizeDefault;

    public PermissionAction Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListMessagesQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListMessagesQuery, Result<MessagePageDto>>
{
    public async Task<Result<MessagePageDto>> Handle(ListMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = requestContext.UserId;
        var caller = await ChannelAccess.CallerAsync(members, request.WorkspaceId, userId, cancellationToken);
        var space = await ChannelAccess.LoadLiveAsync(db, request.WorkspaceId, request.SpaceId, cancellationToken);
        ChannelAccess.RequireVisible(space, caller, userId);

        var messages = db.ChatMessages.AsNoTracking().Where(m => m.SpaceId == space.Id);
        var limit = request.Limit;

        if (request.Before is { } before)
            return Result<MessagePageDto>.Success(await OlderAsync(messages, before, limit, cancellationToken));
        if (request.After is { } after)
            return Result<MessagePageDto>.Success(await NewerAsync(messages, after, limit, cancellationToken));
        if (request.Around is { } around)
            return Result<MessagePageDto>.Success(await AroundAsync(messages, around, limit, cancellationToken));

        return Result<MessagePageDto>.Success(await LatestAsync(messages, limit, cancellationToken));
    }

    private static async Task<MessagePageDto> LatestAsync(IQueryable<Message> messages, int limit, CancellationToken ct)
    {
        var rows = await messages.OrderByDescending(m => m.Sequence).Take(limit + 1).ToListAsync(ct);
        var hasOlder = rows.Count > limit;
        return Page(rows.Take(limit).Reverse().ToList(), hasOlder, hasNewer: false);
    }

    private static async Task<MessagePageDto> OlderAsync(IQueryable<Message> messages, long before, int limit, CancellationToken ct)
    {
        var rows = await messages.Where(m => m.Sequence < before)
            .OrderByDescending(m => m.Sequence).Take(limit + 1).ToListAsync(ct);
        var hasNewer = await messages.AnyAsync(m => m.Sequence >= before, ct);
        return Page(rows.Take(limit).Reverse().ToList(), rows.Count > limit, hasNewer);
    }

    private static async Task<MessagePageDto> NewerAsync(IQueryable<Message> messages, long after, int limit, CancellationToken ct)
    {
        var rows = await messages.Where(m => m.Sequence > after)
            .OrderBy(m => m.Sequence).Take(limit + 1).ToListAsync(ct);
        var hasOlder = await messages.AnyAsync(m => m.Sequence <= after, ct);
        return Page(rows.Take(limit).ToList(), hasOlder, rows.Count > limit);
    }

    private static async Task<MessagePageDto> AroundAsync(IQueryable<Message> messages, long around, int limit, CancellationToken ct)
    {
        var lowerTake = Math.Max(1, limit / 2);
        var lowerRows = await messages.Where(m => m.Sequence <= around)
            .OrderByDescending(m => m.Sequence).Take(lowerTake + 1).ToListAsync(ct);
        var lower = lowerRows.Take(lowerTake).Reverse().ToList();

        var upperTake = limit - lower.Count;
        var upperRows = await messages.Where(m => m.Sequence > around)
            .OrderBy(m => m.Sequence).Take(upperTake + 1).ToListAsync(ct);
        var upper = upperRows.Take(upperTake).ToList();

        return Page([.. lower, .. upper], lowerRows.Count > lowerTake, upperRows.Count > upperTake);
    }

    private static MessagePageDto Page(IReadOnlyList<Message> rows, bool hasOlder, bool hasNewer) =>
        new(rows.Select(MessageDto.From).ToList(), hasOlder, hasNewer);
}

public class ListMessagesQueryValidator : AbstractValidator<ListMessagesQuery>
{
    public ListMessagesQueryValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, ChatLimits.PageSizeMax)
            .WithMessage($"limit must be between 1 and {ChatLimits.PageSizeMax}.");
        RuleFor(x => x)
            .Must(q => new[] { q.Before, q.After, q.Around }.Count(c => c is not null) <= 1)
            .WithMessage("Give at most one of before, after and around.");
        RuleFor(x => x.Before).GreaterThanOrEqualTo(1).When(x => x.Before is not null);
        RuleFor(x => x.After).GreaterThanOrEqualTo(0).When(x => x.After is not null);
        RuleFor(x => x.Around).GreaterThanOrEqualTo(1).When(x => x.Around is not null);
    }
}
