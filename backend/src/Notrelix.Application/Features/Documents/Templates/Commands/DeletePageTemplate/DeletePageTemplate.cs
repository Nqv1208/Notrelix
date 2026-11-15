using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Templates.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Documents.Templates;

namespace Notrelix.Application.Features.Documents.Templates.Commands.DeletePageTemplate;

/// <summary>
/// Deletes a template for good. The creator or a Workspace Admin may; pages made from it are ordinary pages and stay;
/// a stale expected version is 412.
/// </summary>
public record DeletePageTemplateCommand(Guid WorkspaceId, Guid TemplateId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IExpectedVersionRequest
{
    // Any member who can see the workspace may ask; the handler narrows it to the creator or an Admin.
    public PermissionAction Action => PermissionAction.ViewWorkspace;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("documents.page-template"), TemplateId, WorkspaceId);
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class DeletePageTemplateCommandHandler : IRequestHandler<DeletePageTemplateCommand, Result>
{
    private readonly IDocumentDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IWorkspaceMemberFacts _members;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeletePageTemplateCommandHandler(IDocumentDbContext context, ICurrentRequestContext requestContext, IWorkspaceMemberFacts members, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _members = members;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeletePageTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.PageTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId, ct)
            ?? throw new NotFoundException(nameof(PageTemplate), request.TemplateId);
        var userId = _requestContext.UserId;
        await PageTemplateManagers.RequireAsync(_members, request.WorkspaceId, template, userId, ct);
        _context.PageTemplates.Remove(template);
        return Result.Success();
    }
}

public class DeletePageTemplateCommandValidator : AbstractValidator<DeletePageTemplateCommand>
{
    public DeletePageTemplateCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.ExpectedVersion).MustBeAnAuthoritativeVersion();
    }
}
