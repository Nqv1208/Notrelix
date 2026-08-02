using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Infrastructure.Services;

public sealed class ResourceScopeResolver : IResourceScopeResolver
{
    private readonly IWorkManagementDbContext _workDb;
    private readonly IDocumentDbContext _docDb;
    private readonly ICollaborationDbContext _collabDb;
    private readonly IGovernanceDbContext _govDb;
    private readonly IAutomationDbContext _autoDb;

    public ResourceScopeResolver(
        IWorkManagementDbContext workDb,
        IDocumentDbContext docDb,
        ICollaborationDbContext collabDb,
        IGovernanceDbContext govDb,
        IAutomationDbContext autoDb)
    {
        _workDb = workDb;
        _docDb = docDb;
        _collabDb = collabDb;
        _govDb = govDb;
        _autoDb = autoDb;
    }

    public async Task<ResourceScopeSnapshot?> ResolveAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (!LegacyResourceTypeMappings.TryToLegacyEnum(resource.Kind.Value, out var resourceType))
            return null;

        return resourceType switch
        {
            ResourceType.Board => await _workDb.Boards
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Board, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.BoardGroup => await _workDb.BoardGroups
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.BoardGroup, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.BoardField => await _workDb.BoardFields
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.BoardField, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.BoardView => await _workDb.BoardViews
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.BoardView, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.BoardItem => await _workDb.BoardItems
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.BoardItem, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.Label => await _workDb.Labels
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Label, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.Checklist => await _workDb.Checklists
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Checklist, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.ChecklistItem => await (
                from ci in _workDb.ChecklistItems.IgnoreQueryFilters()
                join c in _workDb.Checklists.IgnoreQueryFilters() on ci.ChecklistId equals c.Id
                where ci.Id == resource.ResourceId
                select new ResourceScopeSnapshot(c.AccountId, c.WorkspaceId, ResourceType.ChecklistItem, ci.Id)
            ).FirstOrDefaultAsync(cancellationToken),

            ResourceType.Page => await _docDb.Pages
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Page, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.Block => await _docDb.Blocks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Block, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.Comment => await _collabDb.Comments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Comment, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.Attachment => await _collabDb.Attachments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.Attachment, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.ResourcePermission => await _govDb.ResourcePermissions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.ResourcePermission, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.ShareLink => await _govDb.ShareLinks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.ShareLink, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.AutomationRule => await _autoDb.AutomationRules
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.AutomationRule, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            ResourceType.AutomationExecution => await _autoDb.AutomationExecutions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceScopeSnapshot(r.AccountId, r.WorkspaceId, ResourceType.AutomationExecution, r.Id))
                .FirstOrDefaultAsync(cancellationToken),

            _ => null
        };
    }
}
