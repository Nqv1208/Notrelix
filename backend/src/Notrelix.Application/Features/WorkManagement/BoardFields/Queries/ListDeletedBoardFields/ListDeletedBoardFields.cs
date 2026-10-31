using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Queries.ListDeletedBoardFields;

/// <param name="ValueCount">Live items that still hold a value of the field; a restore brings them back.</param>
public sealed record DeletedBoardFieldDto(Guid Id, string Name, string FieldType, DateTimeOffset DeletedAt, DateTimeOffset RestorableUntil, Guid? DeletedBy = null, string? DeletedByName = null, int ValueCount = 0);

/// <summary>The custom fields deleted from a board in the last 30 days, newest first (Manager, like the delete).</summary>
public record ListDeletedBoardFieldsQuery(Guid BoardId) : IQuery<List<DeletedBoardFieldDto>>, IRequirePermission, IAuthenticatedRequest, IResourceScopedRequest, IReadRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class ListDeletedBoardFieldsQueryHandler : IRequestHandler<ListDeletedBoardFieldsQuery, List<DeletedBoardFieldDto>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _timeProvider;
    private readonly IActorLookupService _actorLookup;

    public ListDeletedBoardFieldsQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider, IActorLookupService actorLookup)
    {
        _actorLookup = actorLookup;
        _context = context;
        _requestContext = requestContext;
        _timeProvider = timeProvider;
    }

    public async Task<List<DeletedBoardFieldDto>> Handle(ListDeletedBoardFieldsQuery request, CancellationToken ct)
    {
        var accountId = _requestContext.RequireAccountId();
        var workspaceId = _requestContext.RequireWorkspaceId();
        var since = _timeProvider.UtcNow - BoardFieldTrashPolicy.RestoreWindow;
        var rows = await _context.BoardFields
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => f.AccountId == accountId && f.WorkspaceId == workspaceId && f.BoardId == request.BoardId && f.DeletedAt != null && f.DeletedAt >= since)
            .OrderByDescending(f => f.DeletedAt)
            .Take(200)
            .Select(f => new { f.Id, f.Name, f.Type, f.DeletedAt, f.DeletedBy })
            .ToListAsync(ct);

        var fieldIds = rows.Select(r => r.Id).ToList();
        var counts = fieldIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.BoardItemValues
                .AsNoTracking()
                .Where(value => fieldIds.Contains(value.FieldId))
                .Join(
                    _context.BoardItems.AsNoTracking().Where(item => item.BoardId == request.BoardId && item.DeletedAt == null && !item.IsArchived),
                    value => value.ItemId,
                    item => item.Id,
                    (value, item) => value.FieldId)
                .GroupBy(fieldId => fieldId)
                .Select(group => new { FieldId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.FieldId, row => row.Count, ct);

        var actors = (await _actorLookup.FindManyAsync(rows.Where(r => r.DeletedBy != null).Select(r => r.DeletedBy!.Value).Distinct().ToList(), ct))
            .ToDictionary(actor => actor.UserId);
        return rows
            .Select(r => new DeletedBoardFieldDto(
                r.Id, r.Name, r.Type.ToString(), r.DeletedAt!.Value, r.DeletedAt.Value + BoardFieldTrashPolicy.RestoreWindow,
                r.DeletedBy, r.DeletedBy is { } by && actors.TryGetValue(by, out var actor) ? actor.Name : null,
                counts.GetValueOrDefault(r.Id)))
            .ToList();
    }
}
