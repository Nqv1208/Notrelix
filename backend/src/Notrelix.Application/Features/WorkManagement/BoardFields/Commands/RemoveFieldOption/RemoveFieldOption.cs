using BoardFieldEntity = global::Notrelix.Domain.WorkManagement.Fields.BoardField;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;

[IdempotencyOperation("work-management.board-fields.remove-field-option.v1")]
public record RemoveFieldOptionCommand(
    Guid BoardId,
    Guid FieldId,
    Guid OptionId,
    long ExpectedVersion,
    Guid? ReplacementOptionId = null) : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
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
            .Include(f => f.Options)
            .FirstOrDefaultAsync(f => f.Id == request.FieldId && f.BoardId == request.BoardId, ct);
        if (field is null) throw new NotFoundException(nameof(BoardFieldEntity), request.FieldId);

        var now = _dateTimeProvider.UtcNow;
        var userId = _currentUser.UserId;
        var isSingleOption = field.Type is FieldType.Status or FieldType.Select;

        FieldOption? replacement = null;
        if (request.ReplacementOptionId is { } replacementId)
        {
            if (!isSingleOption)
                return Result.Failure(new ApplicationError("work.field-option.replacement-not-supported", "Only Status and Select options can be replaced when removed.", ApplicationErrorType.Validation));
            replacement = field.Options.FirstOrDefault(o => o.Id == replacementId && o.Id != request.OptionId);
            if (replacement is null)
                return Result.Failure(new ApplicationError("work.field-option.replacement-invalid", "The replacement must be another option of the same field.", ApplicationErrorType.Validation));
        }

        var itemIds = new List<Guid>();
        if (isSingleOption)
        {
            itemIds = (await _context.BoardItemValues
                    .AsNoTracking()
                    .Where(v => v.FieldId == field.Id)
                    .Select(v => new { v.ItemId, v.Value })
                    .ToListAsync(ct))
                .Where(v => WorkItemSemanticFields.ReadOptionId(v.Value) == request.OptionId)
                .Select(v => v.ItemId)
                .Distinct()
                .ToList();

            // A Status always has a value to land on; a Select may be cleared.
            if (itemIds.Count > 0 && replacement is null && field.Type == FieldType.Status)
                return Result.Failure(new ApplicationError("work.field-option.replacement-required", "Items use this status. Choose the status they move to.", ApplicationErrorType.BusinessRule));
        }

        // Domain rules (last Todo / Done option, default value reference) run before
        // any Item is touched: a refusal leaves Items unchanged.
        field.RemoveOption(request.OptionId, userId, now);
        if (itemIds.Count == 0) return Result.Success();

        var items = await _context.BoardItems
            .Include(i => i.FieldValues)
            .Where(i => itemIds.Contains(i.Id) && i.BoardId == request.BoardId)
            .ToListAsync(ct);
        var newValue = replacement is null
            ? FieldValue.Empty()
            : FieldValue.Create(JsonValue.Create(System.Text.Json.JsonSerializer.Serialize(replacement.Id.ToString())));
        var statusField = field.Type == FieldType.Status
            ? await BoardItemFieldWrites.LoadStatusFieldAsync(_context, request.BoardId, ct)
            : null;
        foreach (var item in items)
        {
            if (item.DeletedAt is null && !item.IsArchived)
            {
                BoardItemFieldWrites.Write(item, field, newValue, statusField?.Id == field.Id ? field : null, userId, now);
                continue;
            }

            // Deleted or archived Items cannot go through the aggregate; their value is
            // repointed so a restore does not bring back a status that no longer exists.
            item.FieldValues.First(v => v.FieldId == field.Id).UpdateValue(newValue);
        }

        return Result.Success();
    }
}
