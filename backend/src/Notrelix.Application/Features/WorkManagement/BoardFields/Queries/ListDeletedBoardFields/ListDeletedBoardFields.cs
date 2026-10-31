using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Queries.ListDeletedBoardFields;

public sealed record DeletedBoardFieldDto(Guid Id, string Name, string FieldType, DateTimeOffset DeletedAt, DateTimeOffset RestorableUntil);

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

    public ListDeletedBoardFieldsQueryHandler(IWorkManagementDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider timeProvider)
    {
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
            .Select(f => new { f.Id, f.Name, f.Type, f.DeletedAt })
            .ToListAsync(ct);
        return rows
            .Select(r => new DeletedBoardFieldDto(r.Id, r.Name, r.Type.ToString(), r.DeletedAt!.Value, r.DeletedAt.Value + BoardFieldTrashPolicy.RestoreWindow))
            .ToList();
    }
}
