using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.ReorderFieldOptions;

[IdempotencyOperation("work-management.board-fields.reorder-field-options.v1")]
public record ReorderFieldOptionsCommand(
    Guid BoardId,
    Guid FieldId,
    List<Guid> OrderedOptionIds, string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardField, FieldId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"reorder-field-options:{FieldId}";
}

public class ReorderFieldOptionsCommandHandler : IRequestHandler<ReorderFieldOptionsCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReorderFieldOptionsCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(ReorderFieldOptionsCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        field.ReorderOptions(request.OrderedOptionIds, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
