using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Infrastructure.Services;

/// <summary>
/// Resolves the tenant scope of a resource by canonical <see cref="ResourceKind"/>.
/// Keyed directly on the kind value — there is no reverse mapping to a legacy enum.
/// Unknown kinds return null so callers fail closed.
/// </summary>
public sealed class ResourceLocator : IResourceLocator
{
    private static readonly ResourceKind Board = ResourceKind.Create("work-management.board");
    private static readonly ResourceKind BoardGroup = ResourceKind.Create("work-management.board-group");
    private static readonly ResourceKind BoardField = ResourceKind.Create("work-management.board-field");
    private static readonly ResourceKind BoardView = ResourceKind.Create("work-management.board-view");
    private static readonly ResourceKind BoardItem = ResourceKind.Create("work-management.board-item");
    private static readonly ResourceKind Label = ResourceKind.Create("work-management.label");
    private static readonly ResourceKind Checklist = ResourceKind.Create("work-management.checklist");
    private static readonly ResourceKind ChecklistItem = ResourceKind.Create("work-management.checklist-item");
    private static readonly ResourceKind Page = ResourceKind.Create("documents.page");
    private static readonly ResourceKind Block = ResourceKind.Create("documents.block");
    private static readonly ResourceKind Comment = ResourceKind.Create("collaboration.comment");
    private static readonly ResourceKind Attachment = ResourceKind.Create("collaboration.attachment");
    private static readonly ResourceKind ResourcePermission = ResourceKind.Create("governance.resource-permission");
    private static readonly ResourceKind ShareLink = ResourceKind.Create("governance.share-link");
    private static readonly ResourceKind AutomationRule = ResourceKind.Create("automation.rule");
    private static readonly ResourceKind AutomationExecution = ResourceKind.Create("automation.execution");

    private readonly IWorkManagementDbContext _workDb;
    private readonly IDocumentDbContext _docDb;
    private readonly ICollaborationDbContext _collabDb;
    private readonly IGovernanceDbContext _govDb;
    private readonly IAutomationDbContext _autoDb;

    public ResourceLocator(
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

    public async Task<ResourceLocation?> LocateAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return resource.Kind.Value switch
        {
            "work-management.board" => ToLocation(Board, await _workDb.Boards
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.board-group" => ToLocation(BoardGroup, await _workDb.BoardGroups
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.board-field" => ToLocation(BoardField, await _workDb.BoardFields
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.board-view" => ToLocation(BoardView, await _workDb.BoardViews
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.board-item" => ToLocation(BoardItem, await _workDb.BoardItems
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.label" => ToLocation(Label, await _workDb.Labels
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.checklist" => ToLocation(Checklist, await _workDb.Checklists
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "work-management.checklist-item" => ToLocation(ChecklistItem, await (
                from ci in _workDb.ChecklistItems.IgnoreQueryFilters()
                join c in _workDb.Checklists.IgnoreQueryFilters() on ci.ChecklistId equals c.Id
                where ci.Id == resource.ResourceId
                select new LocatedRow(ci.Id, c.AccountId, c.WorkspaceId)
            ).FirstOrDefaultAsync(cancellationToken)),

            "documents.page" => ToLocation(Page, await _docDb.Pages
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "documents.block" => ToLocation(Block, await _docDb.Blocks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "collaboration.comment" => ToLocation(Comment, await _collabDb.Comments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "collaboration.attachment" => ToLocation(Attachment, await _collabDb.Attachments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "governance.resource-permission" => ToLocation(ResourcePermission, await _govDb.ResourcePermissions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "governance.share-link" => ToLocation(ShareLink, await _govDb.ShareLinks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "automation.rule" => ToLocation(AutomationRule, await _autoDb.AutomationRules
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            "automation.execution" => ToLocation(AutomationExecution, await _autoDb.AutomationExecutions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new LocatedRow(r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken)),

            _ => null
        };
    }

    private static ResourceLocation? ToLocation(ResourceKind kind, LocatedRow? row) =>
        row is null ? null : new ResourceLocation(kind, row.Id, row.AccountId, row.WorkspaceId);

    private sealed record LocatedRow(Guid Id, Guid AccountId, Guid WorkspaceId);
}
