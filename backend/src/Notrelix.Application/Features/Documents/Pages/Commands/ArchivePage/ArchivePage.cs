using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

/// <summary>
/// Moves a page to the trash together with the active pages under it, as one unit of work (a child never
/// stays behind as a root). The caller must be allowed to trash every page the cascade changes: a child can
/// carry narrower access, and one refused page refuses the whole operation. All pages get the same archive
/// time, which is how a restore tells what went to the trash together. An already trashed page is a no-op.
/// </summary>
public record ArchivePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ArchivePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class ArchivePageCommandHandler : IRequestHandler<ArchivePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly IPageSubtreeAuthorizer _subtree;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchivePageCommandHandler(
        IPageSubtreeStore pages,
        IPageSubtreeAuthorizer subtree,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
        _subtree = subtree;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchivePageCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var page = await _pages.FindAsync(workspaceId, request.PageId, ct);
        if (page is null || page.IsDeleted) throw new NotFoundException(nameof(Page), request.PageId);

        // Page.Archive owns the lifecycle: an already-archived page is a semantic
        // no-op (no version bump, no second event).
        if (page.Status == PageStatus.Archived) return Result.Success();

        var below = (await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
            .Where(descendant => !descendant.IsDeleted && descendant.Status == PageStatus.Active)
            .ToList();
        await _subtree.EnsureAllowedAsync(below.Select(descendant => descendant.Id), id => new ArchivePageCommand(id), ct);

        var now = _dateTimeProvider.UtcNow;
        foreach (var descendant in below) descendant.Archive(_requestContext.UserId, now);
        page.Archive(_requestContext.UserId, now);
        return Result.Success();
    }
}
