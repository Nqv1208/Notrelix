using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.ArchivePage;

/// <summary>
/// Moves a page to the trash together with its whole page subtree, as one unit of work (a child never stays
/// behind as a root). An already trashed page is a no-op, but the subtree is still swept so a retry or a page
/// created under it meanwhile ends up trashed too.
/// </summary>
public record ArchivePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ArchivePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class ArchivePageCommandHandler : IRequestHandler<ArchivePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchivePageCommandHandler(IPageSubtreeStore pages, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
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
        var now = _dateTimeProvider.UtcNow;
        foreach (var descendant in await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
            if (!descendant.IsDeleted) descendant.Archive(_requestContext.UserId, now);
        page.Archive(_requestContext.UserId, now);
        return Result.Success();
    }
}
