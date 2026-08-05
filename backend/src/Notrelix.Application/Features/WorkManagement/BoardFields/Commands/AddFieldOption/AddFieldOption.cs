using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;

[IdempotencyOperation("work-management.board-fields.add-field-option.v1")]
public record AddFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    string Name,
    string Color,
    string? Position) : ICommand<Result<Guid>>, ITransactionalRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
}

public class AddFieldOptionCommandHandler : IRequestHandler<AddFieldOptionCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddFieldOptionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<Guid>> Handle(AddFieldOptionCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        var now = _dateTimeProvider.UtcNow;
        var color = Color.Create(request.Color);
        var position = request.Position is not null
            ? FractionalIndex.Create(request.Position)
            : FractionalIndex.Create("z");

        field.AddOption(request.Name, color, position, _currentUser.UserId, now);

        var option = field.Options.LastOrDefault(o => o.Name == request.Name.Trim());
        return Result<Guid>.Success(option?.Id ?? Guid.Empty);
    }
}
