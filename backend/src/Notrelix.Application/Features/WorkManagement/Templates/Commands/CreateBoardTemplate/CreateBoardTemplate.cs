using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;

[IdempotencyOperation("work-management.templates.create-board-template.v1")]
public record CreateBoardTemplateCommand(
    Guid BoardId,
    string Name,
    string? Description)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.ManageBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardTemplateCommandHandler : IRequestHandler<CreateBoardTemplateCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardTemplateCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardTemplateCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        var now = _dateTimeProvider.UtcNow;
        var structure = JsonValue.Create("{}")!;

        var template = BoardTemplate.Create(request.Name, structure, now, board.WorkspaceId);

        _context.BoardTemplates.Add(template);
        return Result<Guid>.Success(template.Id);
    }
}
