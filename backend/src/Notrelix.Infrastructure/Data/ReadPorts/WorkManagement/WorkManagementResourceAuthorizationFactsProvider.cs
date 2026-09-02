using Notrelix.Application.Common.Security;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Infrastructure.Data.ReadPorts.WorkManagement;

/// <summary>
/// WorkManagement-owned implementation of the transport-neutral resource authorization facts
/// SPI (<see cref="IResourceAuthorizationFactsProvider"/>). It owns the
/// <see cref="IWorkManagementDbContext"/> knowledge for the <c>work-management.*</c> resources
/// and resolves both resource scope (ownership tuple) and Board authorization facts
/// (existence, visibility/audience, actor→Board role). It returns source-owner facts only —
/// it never evaluates a policy decision.
/// </summary>
public sealed class WorkManagementResourceAuthorizationFactsProvider : IResourceAuthorizationFactsProvider
{
    private const string BoardKind = "work-management.board";
    private const string BoardGroupKind = "work-management.board-group";
    private const string BoardFieldKind = "work-management.board-field";
    private const string BoardViewKind = "work-management.board-view";
    private const string BoardItemKind = "work-management.board-item";
    private const string LabelKind = "work-management.label";
    private const string ChecklistKind = "work-management.checklist";
    private const string ChecklistItemKind = "work-management.checklist-item";

    private readonly IWorkManagementDbContext _context;

    public WorkManagementResourceAuthorizationFactsProvider(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<ResourceAuthorizationFacts?> ResolveAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        return resource.Kind.Value switch
        {
            BoardKind => await ResolveBoardAsync(resource, actorUserId, cancellationToken),
            BoardGroupKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.BoardGroups, resource.ResourceId, cancellationToken)),
            BoardFieldKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.BoardFields, resource.ResourceId, cancellationToken)),
            BoardViewKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.BoardViews, resource.ResourceId, cancellationToken)),
            BoardItemKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.BoardItems, resource.ResourceId, cancellationToken)),
            LabelKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.Labels, resource.ResourceId, cancellationToken)),
            ChecklistKind => await ResolveScopeAsync(resource,
                () => FirstScopedAsync(_context.Checklists, resource.ResourceId, cancellationToken)),
            ChecklistItemKind => await ResolveChecklistItemAsync(resource, cancellationToken),
            _ => null
        };
    }

    private async Task<ResourceAuthorizationFacts> ResolveBoardAsync(
        ResourceRef resource,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var located = await _context.Boards
            .IgnoreQueryFilters()
            .Where(board => board.Id == resource.ResourceId)
            .Select(board => new
            {
                board.AccountId,
                board.WorkspaceId,
                board.IsDeleted,
                board.IsArchived,
                board.Visibility,
                MemberRole = _context.BoardMembers
                    .IgnoreQueryFilters()
                    .Where(member => member.BoardId == board.Id && member.UserId == actorUserId)
                    .Select(member => member.Role.ToString())
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (located is null)
        {
            return new ResourceAuthorizationFacts(
                resource.ResourceId, Guid.Empty, Guid.Empty, Exists: false, Audience: null, MemberRole: null);
        }

        var exists = !located.IsDeleted && !located.IsArchived;
        return new ResourceAuthorizationFacts(
            resource.ResourceId,
            located.AccountId,
            located.WorkspaceId,
            Exists: exists,
            Audience: located.Visibility.ToString(),
            MemberRole: located.MemberRole);
    }

    private async Task<ResourceAuthorizationFacts> ResolveChecklistItemAsync(
        ResourceRef resource,
        CancellationToken cancellationToken)
    {
        var scopedRow = await (
            from item in _context.ChecklistItems.IgnoreQueryFilters()
            join checklist in _context.Checklists.IgnoreQueryFilters() on item.ChecklistId equals checklist.Id
            where item.Id == resource.ResourceId
            select new { checklist.AccountId, checklist.WorkspaceId }
        ).FirstOrDefaultAsync(cancellationToken);

        return scopedRow is null
            ? new ResourceAuthorizationFacts(resource.ResourceId, Guid.Empty, Guid.Empty, Exists: false, Audience: null, MemberRole: null)
            : new ResourceAuthorizationFacts(resource.ResourceId, scopedRow.AccountId, scopedRow.WorkspaceId, Exists: true, Audience: null, MemberRole: null);
    }

    private static async Task<ResourceAuthorizationFacts> ResolveScopeAsync(
        ResourceRef resource,
        Func<Task<(Guid AccountId, Guid WorkspaceId)?>> locate)
    {
        var scoped = await locate();
        return scoped.HasValue
            ? new ResourceAuthorizationFacts(resource.ResourceId, scoped.Value.AccountId, scoped.Value.WorkspaceId, Exists: true, Audience: null, MemberRole: null)
            : new ResourceAuthorizationFacts(resource.ResourceId, Guid.Empty, Guid.Empty, Exists: false, Audience: null, MemberRole: null);
    }

    private static async Task<(Guid, Guid)?> FirstScopedAsync<T>(
        DbSet<T> set,
        Guid resourceId,
        CancellationToken cancellationToken)
        where T : Entity, IWorkspaceScoped
    {
        var row = await set
            .IgnoreQueryFilters()
            .Where(entity => entity.Id == resourceId)
            .Select(entity => new { entity.AccountId, entity.WorkspaceId })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : (row.AccountId, row.WorkspaceId);
    }
}
