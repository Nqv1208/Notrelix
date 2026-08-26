using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;

[IdempotencyOperation("work-management.board-fields.remove-field-option.v1")]
public record RemoveFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
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
