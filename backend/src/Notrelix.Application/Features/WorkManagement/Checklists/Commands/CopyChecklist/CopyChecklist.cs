using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.CopyChecklist;

/// <summary>
/// Creates a checklist on an Item that holds a copy of the items of another checklist on the same board, in one
/// change: either the whole copy exists or nothing does. Copied items start open. A source checklist on another
/// board, or one that is gone, is not found.
/// </summary>
[IdempotencyOperation("work-management.checklists.copy-checklist.v1")]
public record CopyChecklistCommand(Guid BoardItemId, Guid SourceChecklistId, string? Title = null)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class CopyChecklistCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    WorkPlacementResolver placement) : IRequestHandler<CopyChecklistCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CopyChecklistCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var source = await ChecklistAggregateLoader.LoadAsync(context, request.SourceChecklistId, ct);
        var sourceOnSameBoard = await context.BoardItems.AsNoTracking()
            .AnyAsync(i => i.Id == source.ItemId && i.BoardId == item.BoardId && i.DeletedAt == null, ct);
        if (!sourceOnSameBoard) throw new NotFoundException(nameof(Checklist), request.SourceChecklistId);

        var position = await placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.ItemChecklists, item.Id),
            WorkPlacementIntent.Append,
            token => ItemChecklistSiblings.LoadAsync(context, item.Id, token),
            ct);

        var now = clock.UtcNow;
        var userId = requestContext.UserId;
        var title = string.IsNullOrWhiteSpace(request.Title) ? source.Title : request.Title;
        var copy = Checklist.Create(requestContext.RequireAccountId(), item.WorkspaceId, item.Id, title, position, userId, now);
        foreach (var sourceItem in source.Items.OrderBy(i => i.Position).ThenBy(i => i.Id))
            copy.AddItem(sourceItem.Title, sourceItem.Position, userId, now);

        context.Checklists.Add(copy);
        return Result<Guid>.Success(copy.Id);
    }
}
