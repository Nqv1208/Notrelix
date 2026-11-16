using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Common;
using Notrelix.Application.Features.Documents.Templates.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Commands.UpdatePageTemplate;

/// <summary>
/// Renames a template or changes its description. The creator or a Workspace Admin may; a stale expected version is 412 and
/// changes nothing; the same name and description is a no-op (no version bump).
/// </summary>
public record UpdatePageTemplateCommand(Guid WorkspaceId, Guid TemplateId, string Name, string? Description, long ExpectedVersion)
    : ICommand<Result<PageTemplateDto>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    // Any member who can see the workspace may ask; the handler narrows it to the creator or an Admin.
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page-template"), TemplateId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class UpdatePageTemplateCommandHandler : IRequestHandler<UpdatePageTemplateCommand, Result<PageTemplateDto>>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IWorkspaceMemberFacts _members;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdatePageTemplateCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IWorkspaceMemberFacts members, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _members = members;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<PageTemplateDto>> Handle(UpdatePageTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.PageTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId, ct)
            ?? throw new NotFoundException(nameof(PageTemplate), request.TemplateId);
        var userId = _requestContext.UserId;
        await PageTemplateManagers.RequireAsync(_members, request.WorkspaceId, template, userId, ct);
        template.Rename(request.Name, request.Description, userId, _dateTimeProvider.UtcNow);
        return Result<PageTemplateDto>.Success(PageTemplateMapper.ToDto(template, canManage: true));
    }
}

public class UpdatePageTemplateCommandValidator : AbstractValidator<UpdatePageTemplateCommand>
{
    public UpdatePageTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(1024);
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
