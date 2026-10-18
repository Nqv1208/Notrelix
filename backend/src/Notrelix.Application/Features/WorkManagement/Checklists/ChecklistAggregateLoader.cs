using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Checklists;

/// <summary>
/// Loads the Checklist aggregate with its items (WM-V3-APP-CHK-001). Every
/// ChecklistItem mutation goes through the owning Checklist, so its version,
/// audit and events cover the child change.
/// </summary>
internal static class ChecklistAggregateLoader
{
    public static async Task<Checklist> LoadAsync(IWorkManagementDbContext context, Guid checklistId, CancellationToken ct) =>
        await context.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == checklistId, ct)
        ?? throw new NotFoundException(nameof(Checklist), checklistId);

    public static async Task<Checklist> LoadOwningAsync(IWorkManagementDbContext context, Guid checklistItemId, CancellationToken ct) =>
        await context.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Items.Any(i => i.Id == checklistItemId), ct)
        ?? throw new NotFoundException(nameof(ChecklistItem), checklistItemId);
}
