using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardTemplate;

[IdempotencyOperation("work-management.templates.create-board-template.v1")]
public record CreateBoardTemplateCommand(
    Guid BoardId,
    string Name,
    string? Description,
    bool IncludeItems = false)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    // Saving a Board as a template needs the same right as managing the Board.
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

        var structure = await BoardStructureCapture.CaptureAsync(
            _context, board, request.IncludeItems, includeAssignees: false,
            count => new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_BoardTemplate_TooManyItems,
                $"A template can hold at most {BoardTemplateStructure.MaxItems} items; this board has {count}. Save it without items or archive some first."),
            ct);

        var template = BoardTemplate.Create(
            board.AccountId, board.WorkspaceId, request.Name, request.Description, structure.ToJson(), _dateTimeProvider.UtcNow);

        _context.BoardTemplates.Add(template);
        return Result<Guid>.Success(template.Id);
    }

}
