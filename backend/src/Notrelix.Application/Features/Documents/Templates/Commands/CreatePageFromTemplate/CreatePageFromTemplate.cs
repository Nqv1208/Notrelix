using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.Documents.Rules;
using Notrelix.Domain.SharedKernel.Ordering;

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
    private static readonly HashSet<BlockType> AttachmentBlocks = [BlockType.Image, BlockType.File, BlockType.Video];

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

        var dropped = CopyBlocks(page, PageVersionSnapshot.ParseBlocks(template.BlocksSnapshot.Value), userId, now);
        return Result<PageFromTemplateDto>.Success(new PageFromTemplateDto(page.Id, dropped));
    }

    private int CopyBlocks(Page page, List<SnapshotBlock> blocks, Guid userId, DateTimeOffset now)
    {
        // A dropped block takes the blocks under it along.
        var skipped = new HashSet<Guid>(blocks.Where(b => AttachmentBlocks.Contains(b.Type)).Select(b => b.Id));
        var grew = true;
        while (grew)
        {
            grew = false;
            foreach (var block in blocks)
                if (block.ParentId is { } parent && skipped.Contains(parent) && skipped.Add(block.Id)) grew = true;
        }

        var map = new Dictionary<Guid, Guid>();
        var set = new PageBlockSet(page.AccountId, page.WorkspaceId, page.Id, []);
        foreach (var block in blocks.Where(b => !skipped.Contains(b.Id)))
        {
            var id = Guid.CreateVersion7();
            map[block.Id] = id;
            var content = PageBlockSet.ContentOf(ToElement(block.Content));
            var properties = PageBlockSet.PropertiesOf(ToElement(block.Properties));
            var position = FractionalIndex.Create(block.Position);

            var created = block.ParentId is { } parent && map.TryGetValue(parent, out var newParent)
                ? Block.CreateChild(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, set.PathTo(newParent), properties, id)
                : Block.CreateRoot(page.AccountId, page.WorkspaceId, page.Id, block.Type, content!, position, userId, now, properties, id);
            _context.Blocks.Add(created);
            set.Add(created);
        }

        return skipped.Count;
    }

    private static System.Text.Json.JsonElement ToElement(string json)
    {
        using var document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
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
