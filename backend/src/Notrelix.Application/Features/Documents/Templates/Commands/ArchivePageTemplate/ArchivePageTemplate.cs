using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.DTOs;
using Notrelix.Application.Features.Documents.Templates.Common;
using Notrelix.Application.Features.Documents.Templates.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Commands.ArchivePageTemplate;

/// <summary>
/// Archives a template: it leaves the gallery, and pages already made from it are unchanged. The creator or a Workspace
/// Admin may; archiving an archived template is a no-op; a stale expected version is 412.
/// </summary>
public record ArchivePageTemplateCommand(Guid WorkspaceId, Guid TemplateId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    // Any member who can see the workspace may ask; the handler narrows it to the creator or an Admin.
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page-template"), TemplateId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class ArchivePageTemplateCommandHandler : IRequestHandler<ArchivePageTemplateCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IWorkspaceMemberFacts _members;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchivePageTemplateCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IWorkspaceMemberFacts members, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _members = members;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchivePageTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.PageTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId, ct)
            ?? throw new NotFoundException(nameof(PageTemplate), request.TemplateId);
        var userId = _requestContext.UserId;
        await PageTemplateManagers.RequireAsync(_members, request.WorkspaceId, template, userId, ct);
        template.Archive(userId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}

public class ArchivePageTemplateCommandValidator : AbstractValidator<ArchivePageTemplateCommand>
{
    public ArchivePageTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
