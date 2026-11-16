using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Calls.DTOs;
using Notrelix.Application.Features.Chat.Calls.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Application.Features.Chat.Calls.Queries.ListCalls;

/// <summary>
/// The call history of a space, newest first, cursor paged (SPEC-CHAT-MEDIA-CALLS section 6). The cursor is the opaque nextCursor of
/// the previous page; limit is 1 to 100, default 50. An outsider is NotFound; a malformed cursor is 400.
/// </summary>
public record ListCallsQuery(Guid WorkspaceId, Guid SpaceId, string? Cursor, int Limit)
    : IQuery<Result<CallPageDto>>, IReadRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public const int DefaultLimit = ChatLimits.PageSizeDefault;
    public PermissionAction Action => PermissionAction.ChatMessagePost;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ListCallsQueryHandler(
    IChatDbContext db,
    ICurrentRequestContext requestContext,
    IWorkspaceMemberFacts members) : IRequestHandler<ListCallsQuery, Result<CallPageDto>>
{
    public async Task<Result<CallPageDto>> Handle(ListCallsQuery request, CancellationToken cancellationToken)
    {
        await ChatCallAccess.MemberSpaceAsync(db, members, request.WorkspaceId, request.SpaceId, requestContext.UserId, cancellationToken);

        var query = db.ChatCalls.AsNoTracking().Include(c => c.Participants).Where(c => c.SpaceId == request.SpaceId);
        if (request.Cursor is not null)
        {
            var (startedAt, id) = DecodeCursor(request.Cursor);
            query = query.Where(c => c.StartedAt < startedAt || (c.StartedAt == startedAt && c.Id < id));
        }

        var rows = await query.OrderByDescending(c => c.StartedAt).ThenByDescending(c => c.Id)
            .Take(request.Limit + 1).ToListAsync(cancellationToken);
        var page = rows.Take(request.Limit).ToList();
        var next = rows.Count > request.Limit ? EncodeCursor(page[^1].StartedAt, page[^1].Id) : null;
        return Result<CallPageDto>.Success(new CallPageDto(page.Select(CallDto.From).ToList(), next));
    }

    private static string EncodeCursor(DateTimeOffset startedAt, Guid id) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{startedAt.UtcTicks}.{id:N}"));

    private static (DateTimeOffset StartedAt, Guid Id) DecodeCursor(string cursor)
    {
        try
        {
            var parts = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split('.');
            return (new DateTimeOffset(long.Parse(parts[0]), TimeSpan.Zero), Guid.ParseExact(parts[1], "N"));
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            throw new Notrelix.Application.Common.Exceptions.ValidationException("cursor is not valid.");
        }
    }
}

public class ListCallsQueryValidator : AbstractValidator<ListCallsQuery>
{
    public ListCallsQueryValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, ChatLimits.CallPageMax);
    }
}
