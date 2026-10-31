using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Pages.Commands.CreatePage;

/// <summary>
/// Creates a page at the Workspace root, in a Workspaces Space, or under a
/// parent page. Authorization follows the placement (GOV-028): under a parent,
/// <c>CreatePage</c> on the parent page (so a page cannot be added under one the
/// caller cannot reach); in a Space, <c>CreatePage</c> on the Space (Editor
/// level); otherwise the Workspace-level usage right. A nested page follows its
/// parent's Space (docs/product/workspaces.md §79), so <c>SpaceId</c> must not
/// be combined with <c>ParentId</c>. Pages nest at most <see cref="PageTreeRules.MaxDepth"/> levels. The create is
/// idempotent: a retry with the same key returns the same page.
/// </summary>
[IdempotencyOperation("documents.pages.create-page.v1")]
public record CreatePageCommand(
    Guid WorkspaceId,
    string Title,
    Guid? ParentId,
    Guid? SpaceId = null
) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreatePage;

    public ResourceRef Resource =>
        ParentId is { } parentId ? ResourceRef.Create(ResourceKind.Create("documents.page"), parentId, WorkspaceId)
        : SpaceId is { } spaceId ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), spaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreatePageCommandHandler : IRequestHandler<CreatePageCommand, Result<Guid>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageSpacePlacement _placement;
    private readonly IPageSubtreeStore _tree;
    private readonly IResourceCreatorGrant _creatorGrant;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageCommandHandler(
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

    public async Task<Result<Guid>> Handle(CreatePageCommand request, CancellationToken ct)
    {
        // Workspace existence is verified by workspace-scoped authorization at a higher layer.
        var spaceId = request.SpaceId;
        if (request.ParentId is { } parentId)
        {
            await _tree.LockTreeAsync(request.WorkspaceId, ct);
            var parent = await _context.Pages.AsNoTracking()
                .Where(p => p.Id == parentId && p.WorkspaceId == request.WorkspaceId && p.Status != PageStatus.Archived)
                .Select(p => new { p.SpaceId })
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(Page), parentId);
            spaceId = parent.SpaceId;
            PageTreeRules.EnsureCanNestUnder(await _tree.DepthOfAsync(request.WorkspaceId, parentId, ct));
        }
        else if (spaceId is { } target)
        {
            await _placement.EnsureActiveTargetAsync(request.WorkspaceId, target, ct);
        }

        var page = Page.Create(_requestContext.RequireAccountId(), request.WorkspaceId, request.Title, _requestContext.UserId, _dateTimeProvider.UtcNow, request.ParentId, spaceId);
        _context.Pages.Add(page);

        // The creator is a Manager of the page, so they can share and manage it.
        _creatorGrant.GrantManager(page.AccountId, page.WorkspaceId, "documents.page", page.Id, _requestContext.UserId, _dateTimeProvider.UtcNow);

        return Result<Guid>.Success(page.Id);
    }
}
