using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Application.Common.Idempotency;
namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;

[IdempotencyOperation("work-management.board-fields.create-board-field.v1")]
public record CreateBoardFieldCommand(Guid BoardId, string Name, string FieldType, string? Settings, string? Position, string? IdempotencyKey = null) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.Board, BoardId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"create-field:{BoardId}:{Name}";
}

public class CreateBoardFieldCommandHandler : IRequestHandler<CreateBoardFieldCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateBoardFieldCommandHandler(
        IWorkManagementDbContext context,
        ICurrentRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(CreateBoardFieldCommand request, CancellationToken ct)
    {
        var board = await _context.Boards.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BoardId && !b.IsArchived, ct);
        if (board is null) throw new NotFoundException(nameof(Board), request.BoardId);

        var now = _dateTimeProvider.UtcNow;
        var position = request.Position is not null
            ? FractionalIndex.Create(request.Position)
            : FractionalIndex.Initial();

        var type = Enum.TryParse<FieldType>(request.FieldType, true, out var parsedType)
            ? parsedType
            : FieldType.Text;

        var settings = request.Settings is not null
            ? FieldSettings.Create(JsonValue.Create(request.Settings)!)
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

        _context.BoardFields.Add(column);
        return Result<Guid>.Success(column.Id);
    }
}
