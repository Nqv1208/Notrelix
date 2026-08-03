using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;

[IdempotencyOperation("work-management.board-fields.update-field-option.v1")]
public record UpdateFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    string Name,
    string Color) : ICommand<Result>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceType.BoardField, FieldId);
}

public class UpdateFieldOptionCommandHandler : IRequestHandler<UpdateFieldOptionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateFieldOptionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(UpdateFieldOptionCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        var color = Color.Create(request.Color);
        field.UpdateOption(request.OptionId, request.Name, color, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
