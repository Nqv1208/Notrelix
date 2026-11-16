using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.Documents.Pages.Commands.DuplicatePage;

/// <summary>
/// Copies one page, not its subpages, next to the original: the same parent and Space, a new page with new block
/// ids, and the caller as Manager. Authorized as reading the source (<c>ViewPage</c>, pipeline) and as creating a
/// page at the destination (handler, the same rule creating a page there has). The page and its blocks are
/// written in one unit of work, so a failure leaves no half-made copy. Images, files and videos are not copied;
/// comments, versions, attachments and permissions are not either. A retry with the same key returns the same
/// copy. The title defaults to "Copy of the original".
/// </summary>
[IdempotencyOperation("documents.pages.duplicate-page.v1")]
public record DuplicatePageCommand(Guid WorkspaceId, Guid PageId, string? Title) : ICommand<Result<DuplicatedPageDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ViewPage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId, WorkspaceId);
}

public class DuplicatePageCommandHandler : IRequestHandler<DuplicatePageCommand, Result<DuplicatedPageDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageSpacePlacement _placement;
    private readonly IPageSubtreeStore _tree;
    private readonly IResourceCreatorGrant _creatorGrant;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DuplicatePageCommandHandler(
        IDocumentDbContext context,
        IPageSpacePlacement placement,
        IPageSubtreeStore tree,
        IResourceCreatorGrant creatorGrant,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _placement = placement;
        _tree = tree;
        _creatorGrant = creatorGrant;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<DuplicatedPageDto>> Handle(DuplicatePageCommand request, CancellationToken ct)
    {
        await _tree.LockTreeAsync(request.WorkspaceId, ct);
        var source = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.WorkspaceId == request.WorkspaceId && p.Status != PageStatus.Archived && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        // The copy sits where the original sits, so the caller needs the right to create a page there.
        if (source.ParentId is { } parentId) await _placement.AuthorizeParentAsync(request.WorkspaceId, parentId, ct);
        else await _placement.AuthorizeTargetAsync(request.WorkspaceId, source.SpaceId, ct);

        var live = await _context.Blocks.AsNoTracking()
            .Where(b => b.PageId == source.Id && b.DeletedAt == null)
            .ToListAsync(ct);

        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;
        var title = string.IsNullOrWhiteSpace(request.Title) ? $"Copy of {source.Title}" : request.Title.Trim();
        if (title.Length > 500) title = title[..500];

        var page = Page.Create(_requestContext.RequireAccountId(), request.WorkspaceId, title, userId, now, source.ParentId, source.SpaceId, source.Icon);
        _context.Pages.Add(page);
        _creatorGrant.GrantManager(page.AccountId, page.WorkspaceId, "documents.page", page.Id, userId, now);

        var dropped = PageBlockCopier.Copy(_context, page, PageVersionSnapshot.InTreeOrder(live), userId, now);
        return Result<DuplicatedPageDto>.Success(new DuplicatedPageDto(page.Id, dropped));
    }
}
