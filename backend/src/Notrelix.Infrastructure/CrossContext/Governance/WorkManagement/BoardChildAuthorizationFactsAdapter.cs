using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Authorization;

namespace Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;

/// <summary>
/// Work Management-owned implementation of the Board-child relation consumed
/// by the Governance access facts (GOV-033). One indexed lookup per request
/// through the Work port, on the request's data session; soft-deleted children
/// resolve too (the resource locator also bypasses filters). An Item, and a
/// checklist or checklist item of one, joins its Group to report whether the
/// Group is archived.
/// </summary>
public sealed class BoardChildAuthorizationFactsAdapter : IBoardChildAuthorizationFacts
{
    private readonly IWorkManagementDbContext _context;

    public BoardChildAuthorizationFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<BoardChildScope> ResolveAsync(string resourceKind, Guid resourceId, CancellationToken cancellationToken)
    {
        Guid? boardId;
        var inArchivedGroup = false;
        Guid? privateTo = null;
        switch (resourceKind)
        {
            case "work-management.board-item":
                var item = await (
                    from i in _context.BoardItems.IgnoreQueryFilters().AsNoTracking()
                    join g in _context.BoardGroups.IgnoreQueryFilters().AsNoTracking() on i.GroupId equals g.Id
                    where i.Id == resourceId
                    select new { i.BoardId, g.IsArchived }).FirstOrDefaultAsync(cancellationToken);
                boardId = item?.BoardId;
                inArchivedGroup = item?.IsArchived ?? false;
                break;
            case "work-management.board-group":
                boardId = await _context.BoardGroups.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId).Select(x => (Guid?)x.BoardId).FirstOrDefaultAsync(cancellationToken);
                break;
            case "work-management.board-field":
                boardId = await _context.BoardFields.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId).Select(x => (Guid?)x.BoardId).FirstOrDefaultAsync(cancellationToken);
                break;
            case "work-management.board-view":
                var view = await _context.BoardViews.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId)
                    .Select(x => new { x.BoardId, Private = x.Visibility == BoardViewVisibility.Private, x.CreatedBy })
                    .FirstOrDefaultAsync(cancellationToken);
                boardId = view?.BoardId;
                privateTo = view is { Private: true } ? view.CreatedBy : null;
                break;
            case "work-management.saved-filter":
                var filter = await _context.SavedFilters.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId)
                    .Select(x => new { x.BoardId, Private = x.Visibility == SavedFilterVisibility.Private, x.CreatedBy })
                    .FirstOrDefaultAsync(cancellationToken);
                boardId = filter?.BoardId;
                privateTo = filter is { Private: true } ? filter.CreatedBy : null;
                break;
            case "work-management.form":
                boardId = await _context.Forms.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId).Select(x => (Guid?)x.BoardId).FirstOrDefaultAsync(cancellationToken);
                break;
            case "work-management.form-submission":
                boardId = await _context.FormSubmissions.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId).Select(x => (Guid?)x.BoardId).FirstOrDefaultAsync(cancellationToken);
                break;
            case "work-management.label":
                boardId = await _context.Labels.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == resourceId).Select(x => (Guid?)x.BoardId).FirstOrDefaultAsync(cancellationToken);
                break;
            case "work-management.checklist":
                var checklistScope = await (
                    from checklist in _context.Checklists.IgnoreQueryFilters().AsNoTracking()
                    join parent in _context.BoardItems.IgnoreQueryFilters().AsNoTracking() on checklist.ItemId equals parent.Id
                    join g in _context.BoardGroups.IgnoreQueryFilters().AsNoTracking() on parent.GroupId equals g.Id
                    where checklist.Id == resourceId
                    select new { parent.BoardId, g.IsArchived }).FirstOrDefaultAsync(cancellationToken);
                boardId = checklistScope?.BoardId;
                inArchivedGroup = checklistScope?.IsArchived ?? false;
                break;
            case "work-management.checklist-item":
                var checklistItemScope = await (
                    from checklistItem in _context.ChecklistItems.IgnoreQueryFilters().AsNoTracking()
                    join checklist in _context.Checklists.IgnoreQueryFilters().AsNoTracking() on checklistItem.ChecklistId equals checklist.Id
                    join parent in _context.BoardItems.IgnoreQueryFilters().AsNoTracking() on checklist.ItemId equals parent.Id
                    join g in _context.BoardGroups.IgnoreQueryFilters().AsNoTracking() on parent.GroupId equals g.Id
                    where checklistItem.Id == resourceId
                    select new { parent.BoardId, g.IsArchived }).FirstOrDefaultAsync(cancellationToken);
                boardId = checklistItemScope?.BoardId;
                inArchivedGroup = checklistItemScope?.IsArchived ?? false;
                break;
            default:
                return BoardChildScope.NotBoardChild;
        }

        return new BoardChildScope(true, boardId, inArchivedGroup, privateTo);
    }
}
