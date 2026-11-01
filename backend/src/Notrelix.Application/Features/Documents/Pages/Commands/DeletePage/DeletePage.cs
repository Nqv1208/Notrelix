using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;

/// <summary>
/// Deletes a page for good, with the pages that went to the trash together with it (the same archive time).
/// Only a page already in the trash can be deleted, and only by a Manager of the page (<c>DeletePage</c>); the
/// caller must be allowed to delete every page the cascade changes, and one refused page refuses the whole
/// operation. A page trashed on its own earlier, or an active page below it, is left alone.
/// </summary>
public record DeletePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.DeletePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly IPageSubtreeAuthorizer _subtree;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeletePageCommandHandler(IPageSubtreeStore pages, IPageSubtreeAuthorizer subtree, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
        _subtree = subtree;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeletePageCommand request, CancellationToken ct)
    {
        var workspaceId = _requestContext.RequireWorkspaceId();
        var page = await _pages.FindAsync(workspaceId, request.PageId, ct);
        if (page is null || page.IsDeleted) throw new NotFoundException(nameof(Page), request.PageId);

        if (page.Status != PageStatus.Archived)
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(Notrelix.Domain.Documents.DocumentRuleCodes.Documents_Page_NotInTrash, "Only a page in the trash can be deleted for good.");

        // One archive time marks what went to the trash together (the cascade wrote a single `now`).
        var archivedAt = page.UpdatedAt;
        var together = (await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
            .Where(descendant => archivedAt is not null && !descendant.IsDeleted && descendant.Status == PageStatus.Archived && descendant.UpdatedAt == archivedAt)
            .ToList();
        await _subtree.EnsureAllowedAsync(together.Select(descendant => descendant.Id), id => new DeletePageCommand(id), ct);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        foreach (var descendant in together) descendant.Delete(userId, now);
        page.Delete(userId, now);
        return Result.Success();
    }
}
