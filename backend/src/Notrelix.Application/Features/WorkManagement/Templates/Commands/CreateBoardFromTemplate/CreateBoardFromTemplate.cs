using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Templates.Commands.CreateBoardFromTemplate;

[IdempotencyOperation("work-management.templates.create-board-from-template.v1")]
public record CreateBoardFromTemplateCommand(
    Guid TemplateId,
    Guid WorkspaceId,
    string Name,
    Guid? SpaceId = null)
    : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IWorkspaceRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateBoard;

    // GOV-028: creating in a Space is authorized on that Space (Editor level).
    public ResourceRef Resource => SpaceId is { } spaceId
        ? ResourceRef.Create(ResourceKind.Create("workspaces.space"), spaceId, WorkspaceId)
        : ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId);
}

public class CreateBoardFromTemplateCommandHandler : IRequestHandler<CreateBoardFromTemplateCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly IBoardSpacePlacement _placement;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardFromTemplateCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFromTemplateCommand request, CancellationToken ct)
    {
        var template = await _context.BoardTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) throw new NotFoundException(nameof(BoardTemplate), request.TemplateId);

        var workspace = await _context.Boards.AsNoTracking()
            .Where(b => b.WorkspaceId == request.WorkspaceId)
            .Select(b => new { b.WorkspaceId })
            .FirstOrDefaultAsync(ct);
        if (workspace is null) throw new NotFoundException(nameof(Workspace), request.WorkspaceId);

        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(request.WorkspaceId, spaceId, ct);

        var now = _dateTimeProvider.UtcNow;

        var board = Board.Create(
            _requestContext.RequireAccountId(),
            request.WorkspaceId,
            _requestContext.UserId,
            request.Name,
            template.Description,
            now,
            spaceId: request.SpaceId);

        _context.Boards.Add(board);
        return Result<Guid>.Success(board.Id);
    }
}
