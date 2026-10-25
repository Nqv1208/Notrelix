using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;
using Notrelix.Application.Features.WorkManagement.Public.ItemNotifications;

namespace Notrelix.Infrastructure.CrossContext.Collaboration.WorkManagement;

/// <summary>Work Management-owned implementation of the Item facts consumed by Collaboration's follower notifications.</summary>
public sealed class WorkItemNotificationFactsAdapter : IWorkItemNotificationFacts
{
    private readonly IWorkManagementDbContext _context;

    public WorkItemNotificationFactsAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<WorkItemNotificationFacts?> FindAsync(
        Guid workspaceId, Guid itemId, Guid? changedFieldId, CancellationToken cancellationToken)
    {
        var item = await _context.BoardItems
            .AsNoTracking()
            .Include(i => i.FieldValues)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.WorkspaceId == workspaceId && i.DeletedAt == null, cancellationToken);
        if (item is null) return null;

        var statusField = await BoardItemFieldWrites.LoadStatusFieldAsync(_context, item.BoardId, cancellationToken);
        var isStatus = statusField is not null && changedFieldId == statusField.Id;
        string? statusName = null;
        if (statusField is not null)
        {
            var optionId = WorkItemSemanticFields.ReadOptionId(item.FieldValues.FirstOrDefault(v => v.FieldId == statusField.Id)?.Value);
            statusName = statusField.Options.FirstOrDefault(o => o.Id == optionId)?.Name;
        }

        return new WorkItemNotificationFacts(item.BoardId, item.Name, isStatus, statusName);
    }
}
