using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Blocks.Services;
using Notrelix.Application.Features.Documents.Common;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Pages.Services;
using Notrelix.Application.Features.Documents.Templates.Common;
using Notrelix.Application.Features.Documents.Versions.Services;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Domain.Documents.Templates;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.Documents.Templates.Commands.CreatePageTemplate;

/// <summary>
/// Saves the content of a page as a workspace template ("Save as template"). A person who may edit the page may
/// do it. The template is creation input: it holds a copy of the page's title, icon and blocks, and nothing
/// that happens to the page afterwards changes it (DCT-018).
/// </summary>
public record CreatePageTemplateCommand(Guid WorkspaceId, Guid PageId, string Name, string? Description) : ICommand<Result<PageTemplateDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.UpdatePage;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page"), PageId, WorkspaceId);
}

public class CreatePageTemplateCommandHandler : IRequestHandler<CreatePageTemplateCommand, Result<PageTemplateDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreatePageTemplateCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PageTemplateDto>> Handle(CreatePageTemplateCommand request, CancellationToken ct)
    {
        var page = await _context.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PageId && p.WorkspaceId == request.WorkspaceId && p.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Page), request.PageId);

        var live = await _context.Blocks.AsNoTracking()
            .Where(b => b.PageId == request.PageId && b.DeletedAt == null)
            .ToListAsync(ct);
        var blocks = PageVersionSnapshot.InTreeOrder(live);

        var pageSnapshot = JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(new { title = page.Title, icon = page.Icon }));
        var blocksSnapshot = JsonValue.Create(PageVersionSnapshot.WriteBlocks(blocks));

        var now = _dateTimeProvider.UtcNow;
        var userId = _requestContext.UserId;
        var template = PageTemplate.Create(request.Name, pageSnapshot, blocksSnapshot, now, request.WorkspaceId, userId, request.Description);
        template.Publish(userId, now);
        _context.PageTemplates.Add(template);

        return Result<PageTemplateDto>.Success(PageTemplateMapper.ToDto(template));
    }
}
