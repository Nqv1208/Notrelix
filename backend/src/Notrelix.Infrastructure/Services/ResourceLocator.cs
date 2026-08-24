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
            "work-management.board" => await _workDb.Boards
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Board, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.board-group" => await _workDb.BoardGroups
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(BoardGroup, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.board-field" => await _workDb.BoardFields
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(BoardField, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.board-view" => await _workDb.BoardViews
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(BoardView, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.board-item" => await _workDb.BoardItems
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(BoardItem, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.label" => await _workDb.Labels
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Label, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.checklist" => await _workDb.Checklists
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Checklist, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "work-management.checklist-item" => await (
                from ci in _workDb.ChecklistItems.IgnoreQueryFilters()
                join c in _workDb.Checklists.IgnoreQueryFilters() on ci.ChecklistId equals c.Id
                where ci.Id == resource.ResourceId
                select new ResourceLocation(ChecklistItem, ci.Id, c.AccountId, c.WorkspaceId)
            ).FirstOrDefaultAsync(cancellationToken),

            "documents.page" => await _docDb.Pages
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Page, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "documents.block" => await _docDb.Blocks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Block, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "collaboration.comment" => await _collabDb.Comments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Comment, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "collaboration.attachment" => await _collabDb.Attachments
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(Attachment, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "governance.resource-permission" => await _govDb.ResourcePermissions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(ResourcePermission, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "governance.share-link" => await _govDb.ShareLinks
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(ShareLink, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "automation.rule" => await _autoDb.AutomationRules
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(AutomationRule, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            "automation.execution" => await _autoDb.AutomationExecutions
                .IgnoreQueryFilters()
                .Where(r => r.Id == resource.ResourceId)
                .Select(r => new ResourceLocation(AutomationExecution, r.Id, r.AccountId, r.WorkspaceId))
                .FirstOrDefaultAsync(cancellationToken),

            _ => null
        };
    }
}
