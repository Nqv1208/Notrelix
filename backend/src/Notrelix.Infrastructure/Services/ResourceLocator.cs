using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;

namespace Notrelix.Infrastructure.Services;

/// <summary>
/// Resolves the tenant scope of a resource by canonical <see cref="ResourceKind"/>.
/// Keyed directly on the kind value — there is no reverse mapping to a legacy enum.
/// Unknown kinds return null so callers fail closed.
///
/// WorkManagement resources are routed through the WorkManagement-owned resource
/// authorization facts SPI (<see cref="IResourceAuthorizationFactsProvider"/>) so shared
/// Infrastructure no longer depends on WorkManagement private persistence; other owners
/// (Documents/Collaboration/Governance/Automation) remain resolved here directly this phase.
/// </summary>
public sealed class ResourceLocator : IResourceLocator
{
    private static readonly ResourceKind Page = ResourceKind.Create("documents.page");
    private static readonly ResourceKind Block = ResourceKind.Create("documents.block");
    private static readonly ResourceKind Comment = ResourceKind.Create("collaboration.comment");
    private static readonly ResourceKind Attachment = ResourceKind.Create("collaboration.attachment");
    private static readonly ResourceKind ResourcePermission = ResourceKind.Create("governance.resource-permission");
    private static readonly ResourceKind ShareLink = ResourceKind.Create("governance.share-link");
    private static readonly ResourceKind AutomationRule = ResourceKind.Create("automation.rule");
    private static readonly ResourceKind AutomationExecution = ResourceKind.Create("automation.execution");

    private readonly IResourceAuthorizationFactsProvider _resourceFactsProvider;
    private readonly IDocumentDbContext _docDb;
    private readonly ICollaborationDbContext _collabDb;
    private readonly IGovernanceDbContext _govDb;
    private readonly IAutomationDbContext _autoDb;

    public ResourceLocator(
        IResourceAuthorizationFactsProvider resourceFactsProvider,
        IDocumentDbContext docDb,
        ICollaborationDbContext collabDb,
        IGovernanceDbContext govDb,
        IAutomationDbContext autoDb)
    {
        _resourceFactsProvider = resourceFactsProvider;
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
        switch (resource.Kind.Value)
        {
            case "work-management.board":
            case "work-management.board-group":
            case "work-management.board-field":
            case "work-management.board-view":
            case "work-management.board-item":
            case "work-management.label":
            case "work-management.checklist":
            case "work-management.checklist-item":
            {
                var facts = await _resourceFactsProvider.ResolveAsync(resource, actorUserId, cancellationToken);
                if (facts is null || facts.AccountId == Guid.Empty)
                {
                    return null;
                }

                return new ResourceLocation(
                    resource.Kind,
                    facts.ResourceId,
                    facts.AccountId,
                    facts.WorkspaceId);
            }
        }

        return resource.Kind.Value switch
        {
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
