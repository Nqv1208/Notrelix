using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;

/// <summary>
/// Renames/recolours a field option. For an option of a Status field the optional
/// <see cref="Category"/> changes its work-status category; null keeps it. When
/// the category of an option of the Board's Status field changes, the Items
/// holding that option are completed or reopened in the same transaction.
/// </summary>
[IdempotencyOperation("work-management.board-fields.update-field-option.v1")]
public record UpdateFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    string Name,
    string Color,
    long ExpectedVersion,
    string? Category = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateField;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-field"), FieldId);
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
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        var color = Color.Create(request.Color);
        var categoryBefore = field.Options.FirstOrDefault(option => option.Id == request.OptionId)?.Category;
        var now = _dateTimeProvider.UtcNow;
        field.UpdateOption(request.OptionId, request.Name, color, _currentUser.UserId, now,
            WorkStatusCategoryNames.ParseOptional(request.Category));

        var categoryAfter = field.Options.FirstOrDefault(option => option.Id == request.OptionId)?.Category;
        if (field.Type == FieldType.Status && categoryAfter != categoryBefore)
        {
            await BoardItemFieldWrites.SyncCompletionForOptionAsync(_context, field, request.OptionId, _currentUser.UserId, now, ct);
        }

        return Result.Success();
    }
}
