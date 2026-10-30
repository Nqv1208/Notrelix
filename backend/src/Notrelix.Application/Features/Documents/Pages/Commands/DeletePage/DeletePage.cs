using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Pages.Services;

namespace Notrelix.Application.Features.Documents.Pages.Commands.DeletePage;

/// <summary>
/// Deletes a page for good, with the pages in the trash under it. Only a page already in the trash can be
/// deleted, and only by a Manager of the page (<c>DeletePage</c>). An active page below it (older data that
/// never went to the trash with it) is left alone.
/// </summary>
public record DeletePageCommand(Guid PageId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.DeletePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId);
}

public class DeletePageCommandHandler : IRequestHandler<DeletePageCommand, Result>
{
    private readonly IPageSubtreeStore _pages;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeletePageCommandHandler(IPageSubtreeStore pages, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _pages = pages;
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

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        foreach (var descendant in await _pages.LoadDescendantsAsync(workspaceId, page.Id, ct))
            if (!descendant.IsDeleted && descendant.Status == PageStatus.Archived) descendant.Delete(userId, now);
        page.Delete(userId, now);
        return Result.Success();
    }
}
