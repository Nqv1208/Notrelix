using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.ArchiveBoardTemplate;

[IdempotencyOperation("work-management.templates.archive-board-template.v1")]
public record ArchiveBoardTemplateCommand(Guid TemplateId)
    : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), TemplateId);
}

public class ArchiveBoardTemplateCommandHandler : IRequestHandler<ArchiveBoardTemplateCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ArchiveBoardTemplateCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ArchiveBoardTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.BoardTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) throw new NotFoundException(nameof(BoardTemplate), request.TemplateId);

        template.Archive(_requestContext.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
