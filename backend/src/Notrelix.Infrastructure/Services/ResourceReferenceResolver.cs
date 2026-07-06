using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Infrastructure.Services;

public sealed class ResourceReferenceResolver : IResourceReferenceResolver
{
    private readonly IWorkManagementDbContext _workDb;
    private readonly IDocumentDbContext _docDb;
    private readonly ICollaborationDbContext _collabDb;

    public ResourceReferenceResolver(
        IWorkManagementDbContext workDb,
        IDocumentDbContext docDb,
        ICollaborationDbContext collabDb)
    {
        _workDb = workDb;
        _docDb = docDb;
        _collabDb = collabDb;
    }

    public async Task<Guid?> GetWorkspaceIdAsync(Guid resourceId, string resourceType, CancellationToken ct)
    {
        return resourceType switch
        {
            ResourceTypes.Page => await _docDb.Pages
                .Where(p => p.Id == resourceId)
                .Select(p => (Guid?)p.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            ResourceTypes.Block => await _docDb.Blocks
                .Where(b => b.Id == resourceId)
                .Select(b => (Guid?)b.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            ResourceTypes.BoardItem => await _workDb.BoardItems
                .Where(i => i.Id == resourceId)
                .Select(i => (Guid?)i.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            ResourceTypes.Board => await _workDb.Boards
                .Where(b => b.Id == resourceId)
                .Select(b => (Guid?)b.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            ResourceTypes.Comment => await _collabDb.Comments
                .Where(c => c.Id == resourceId)
                .Select(c => (Guid?)c.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            ResourceTypes.Attachment => await _collabDb.Attachments
                .Where(a => a.Id == resourceId)
                .Select(a => (Guid?)a.WorkspaceId)
                .FirstOrDefaultAsync(ct),

            _ => null
        };
    }

    public async Task<bool> ExistsAsync(Guid resourceId, string resourceType, CancellationToken ct)
    {
        var workspaceId = await GetWorkspaceIdAsync(resourceId, resourceType, ct);
        return workspaceId.HasValue;
    }

    public async Task<AccountContextSnapshot?> GetAccountContextAsync(Guid resourceId, string resourceType, CancellationToken ct)
    {
        return resourceType switch
        {
            ResourceTypes.BoardItem => await _workDb.BoardItems
                .Where(i => i.Id == resourceId && i.DeletedAt == null)
                .Select(i => new AccountContextSnapshot(i.AccountId, i.WorkspaceId))
                .FirstOrDefaultAsync(ct),

            _ => null
        };
    }
}