using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Common.Idempotency;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;

[IdempotencyOperation("work-management.board-fields.remove-field-option.v1")]
public record RemoveFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    string? IdempotencyKey = null) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardField, FieldId);
    string IIdempotentRequest.IdempotencyKey => IdempotencyKey ?? $"remove-field-option:{OptionId}";
}

public class RemoveFieldOptionCommandHandler : IRequestHandler<RemoveFieldOptionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveFieldOptionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(RemoveFieldOptionCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        field.RemoveOption(request.OptionId, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
