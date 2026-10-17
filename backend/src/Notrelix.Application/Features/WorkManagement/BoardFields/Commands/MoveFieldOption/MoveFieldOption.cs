using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;

/// <summary>
/// Normal option drag (WM-V3-APP-OPTION-001): moves one option relative to its
/// neighbors under the field's FieldOptions ordering lock, rewriting only its own
/// key. Replaces the full-list ReorderFieldOptions; moving an option to the place
/// it already occupies is a semantic no-op.
/// </summary>
[IdempotencyOperation("work-management.board-fields.move-field-option.v1")]
public record MoveFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    long ExpectedVersion,
    Guid? PreviousSiblingId = null,
    Guid? NextSiblingId = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
}

public class MoveFieldOptionCommandHandler : IRequestHandler<MoveFieldOptionCommand, Result>
{
    private readonly IWorkManagementDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WorkPlacementResolver _placement;

    public MoveFieldOptionCommandHandler(
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

    public async Task<Result> Handle(MoveFieldOptionCommand request, CancellationToken ct)
    {
        var field = await _context.BoardFields
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);
        if (field.Options.All(o => o.Id != request.OptionId))
            throw new NotFoundException(nameof(FieldOption), request.OptionId);

        var position = await _placement.ResolveAsync(
            new WorkOrderingScope(WorkOrderingScopeKind.FieldOptions, field.Id),
            new WorkPlacementIntent(request.PreviousSiblingId, request.NextSiblingId),
            token => FieldOptionSiblings.LoadAsync(_context, field.Id, token),
            ct,
            movedEntityId: request.OptionId);

        field.MoveOption(request.OptionId, position, _currentUser.UserId, _dateTimeProvider.UtcNow);
        return Result.Success();
    }
}
