using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.Identity.Public.Queries;
using Notrelix.Application.Features.WorkManagement.Boards.Services;
using Notrelix.Application.Features.WorkManagement.Templates.Services;

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
    private readonly IIdentityUserFacts _identity;

    public CreateBoardFromTemplateCommandHandler(
        IWorkManagementDbContext context,
        IBoardSpacePlacement placement,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        IIdentityUserFacts identity)
    {
        _context = context;
        _placement = placement;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _identity = identity;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFromTemplateCommand request, CancellationToken ct)
    {
        var userId = _requestContext.UserId;
        var locale = await _identity.FindLocaleAsync(userId, ct);

        BoardTemplateStructure structure;
        string? description;
        if (BuiltInBoardTemplates.IsBuiltIn(request.TemplateId))
        {
            structure = BuiltInBoardTemplates.Structure(request.TemplateId, locale);
            description = BuiltInBoardTemplates.All(locale).Single(t => t.Id == request.TemplateId).Description;
        }
        else
        {
            // A template of another workspace or one that is not published is not found: the gallery offers
            // only this workspace's published templates.
            var template = await _context.BoardTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId && t.WorkspaceId == request.WorkspaceId, ct);
            if (template is null || template.Status != TemplateStatus.Published)
                throw new NotFoundException(nameof(BoardTemplate), request.TemplateId);
            structure = BoardTemplateStructure.FromJson(template.Structure);
            description = template.Description;
        }

        if (request.SpaceId is { } spaceId)
            await _placement.EnsureActiveTargetAsync(request.WorkspaceId, spaceId, ct);

        var now = _dateTimeProvider.UtcNow;
        var accountId = _requestContext.RequireAccountId();
        var board = Board.Create(accountId, request.WorkspaceId, userId, request.Name, description, now, spaceId: request.SpaceId);
        _context.Boards.Add(board);
        BoardOwnership.AddCreatorAsOwner(_context, board.Id, userId, now);

        BoardStructureInstantiator.Instantiate(_context, board, structure, accountId, request.WorkspaceId, userId, now);

        return Result<Guid>.Success(board.Id);
    }
}
