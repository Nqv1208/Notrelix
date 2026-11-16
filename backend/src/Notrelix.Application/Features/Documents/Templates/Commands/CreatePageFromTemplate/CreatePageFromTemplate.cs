using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Rules;

namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageFromTemplate;

/// <summary>
/// Creates a page from a template: an ordinary page and ordinary blocks with new ids, placed like any new page
/// (under a parent page, in a Space, or at the Workspace root; the same authorization as creating a page there).
/// Images, files and videos are not copied: an attachment belongs to the page it was uploaded to, so a copy
/// would point at content the new page's readers cannot open. The result says how many blocks were left out.
/// </summary>
[IdempotencyOperation("documents.templates.create-page-from-template.v1")]
public record CreatePageFromTemplateCommand(Guid WorkspaceId, Guid TemplateId, string? Title, Guid? ParentId, Guid? SpaceId = null) : ICommand<Result<PageFromTemplateDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreatePage;

    public ResourceRef Resource =>
        ParentId is { } parentId ? ResourceRef.Create(ResourceKind.Create("documents.page"), parentId, WorkspaceId)
        : SpaceId is { } spaceId ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), spaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreatePageFromTemplateCommandHandler : IRequestHandler<CreatePageFromTemplateCommand, Result<PageFromTemplateDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly IPageSpacePlacement _placement;
    private readonly IPageSubtreeStore _tree;
    private readonly IResourceCreatorGrant _creatorGrant;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageFromTemplateCommandHandler(
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

    public async Task<Result<PageFromTemplateDto>> Handle(CreatePageFromTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.PageTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId && t.Status == PageTemplateStatus.Published, ct)
            ?? throw new NotFoundException(nameof(PageTemplate), request.TemplateId);

        var spaceId = request.SpaceId;
        if (request.ParentId is { } parentId)
        {
            await _tree.LockTreeAsync(request.WorkspaceId, ct);
            var parent = await _context.Pages.AsNoTracking()
                .Where(p => p.Id == parentId && p.WorkspaceId == request.WorkspaceId && p.Status != PageStatus.Archived && p.DeletedAt == null)
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

        var (templateTitle, templateIcon) = ReadPageSnapshot(template.PageSnapshot.Value);
        var title = string.IsNullOrWhiteSpace(request.Title) ? templateTitle : request.Title!;
        var accountId = _requestContext.RequireAccountId();
        var userId = _requestContext.UserId;
        var now = _dateTimeProvider.UtcNow;

        var page = Page.Create(accountId, request.WorkspaceId, title, userId, now, request.ParentId, spaceId, templateIcon);
        _context.Pages.Add(page);
        _creatorGrant.GrantManager(page.AccountId, page.WorkspaceId, "documents.page", page.Id, userId, now);

        var dropped = PageBlockCopier.Copy(_context, page, PageVersionSnapshot.ParseBlocks(template.BlocksSnapshot.Value), userId, now);
        return Result<PageFromTemplateDto>.Success(new PageFromTemplateDto(page.Id, dropped));
    }

    internal static (string Title, string Icon) ReadPageSnapshot(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var root = document.RootElement;
        return (
            root.TryGetProperty("title", out var title) ? title.GetString() ?? "Untitled" : "Untitled",
            root.TryGetProperty("icon", out var icon) ? icon.GetString() ?? "" : "");
    }
}
