using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;

/// <summary>
/// Adds an option to a select/status field (WM-V3-APP-OPTION-001). Placement is
/// relative to the requested neighbors (append when both are omitted) and is
/// resolved under the field's FieldOptions ordering lock; the client never sends
/// a raw ordering key.
/// </summary>
[IdempotencyOperation("work-management.board-fields.add-field-option.v1")]
public record AddFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    string Name,
    string Color,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null) : ICommand<Result<Guid>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
}

public class AddFieldOptionCommandHandler : IRequestHandler<AddFieldOptionCommand, Result<Guid>>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public AddFieldOptionCommandHandler(
        IWorkManagementDbContext context,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        WorkPlacementResolver placement)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _placement = placement;
    }

    public async Task<Result<Guid>> Handle(AddFieldOptionCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        var color = Color.Create(request.Color);
        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.FieldOptions, field.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => FieldOptionSiblings.LoadAsync(_context, field.Id, token),
            ct);

        var option = field.AddOption(request.Name, color, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result<Guid>.Success(option.Id);
    }
}
