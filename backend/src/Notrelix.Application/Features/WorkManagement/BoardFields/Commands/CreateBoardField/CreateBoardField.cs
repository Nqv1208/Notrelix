using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

[IdempotencyOperation("work-management.board-fields.create-board-field.v1")]
public record CreateBoardFieldCommand(Guid BoardId, string Name, string FieldType, string? Settings, Guid? PreviousSiblingId = null, Guid? NextSiblingId = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board"), BoardId);
}

public class CreateBoardFieldCommandHandler : IRequestHandler<CreateBoardFieldCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public CreateBoardFieldCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFieldCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        // An unknown type fails closed instead of silently becoming Text (WMREQ027).
        if (!FieldTypeNames.TryParse(request.FieldType, out var type))
            return Result<Guid>.Failure(new ApplicationError(
                "work.board-field.unknown-type", $"Unknown field type '{request.FieldType}'.", ApplicationErrorType.Validation));

        var now = _dateTimeProvider.UtcNow;
        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.BoardFields, board.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => BoardFieldSiblings.LoadAsync(_context, board.Id, token),
            ct);

        var settings = request.Settings is not null
            ? FieldSettings.Create(JsonValue.Create(await ComputedFieldGuard.NormalizeAsync(_context, board.Id, null, type, request.Settings, ct))!)
            : type == FieldType.Status
                ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!)
                : FieldSettings.Empty();

        var column = BoardField.Create(
            _requestContext.RequireAccountId(),
            board.WorkspaceId,
            request.BoardId,
            request.Name,
            type,
            settings,
            position,
            _requestContext.UserId,
            now);

        await ComputedFieldGuard.EnsureAsync(_context, column, ct);
        _context.BoardFields.Add(column);
        return Result<Guid>.Success(column.Id);
    }
}
