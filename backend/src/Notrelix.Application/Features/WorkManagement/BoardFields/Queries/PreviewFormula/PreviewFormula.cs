using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Queries.PreviewFormula;

/// <summary>
/// What a draft formula would show on one Item of the Board, before it is saved: the same engine and the same
/// values as a board read, with the draft evaluated as one more calculated field that is never stored.
/// </summary>
public record PreviewFormulaQuery(Guid BoardId, Guid ItemId, string Expression) : IQuery<ComputedValueDto>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest
{
    public const int MaxExpressionLength = 2000;

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class PreviewFormulaQueryValidator : AbstractValidator<PreviewFormulaQuery>
{
    public PreviewFormulaQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Expression).NotEmpty().MaximumLength(PreviewFormulaQuery.MaxExpressionLength);
    }
}

public class PreviewFormulaQueryHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider time)
    : IRequestHandler<PreviewFormulaQuery, ComputedValueDto>
{
    public async Task<ComputedValueDto> Handle(PreviewFormulaQuery request, CancellationToken cancellationToken)
    {
        var item = await context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.BoardId == request.BoardId && i.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(BoardItem), request.ItemId);

        BoardField draft;
        try
        {
            draft = BoardField.Create(
                item.AccountId, item.WorkspaceId, item.BoardId, "preview", FieldType.Formula,
                FieldSettings.Create(JsonValue.Create(JsonSerializer.Serialize(new { expression = request.Expression }))),
                FractionalIndex.Create("a0"), currentUser.UserId, time.UtcNow);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException)
        {
            // A formula that does not parse has no value to show; the save would refuse it by the same rule.
            return new ComputedValueDto("error", null, BoardItemFieldProjection.ErrInvalidDefinition);
        }

        var fields = await context.BoardFields.AsNoTracking()
            .Include(f => f.Options)
            .Where(f => f.BoardId == request.BoardId && f.DeletedAt == null)
            .ToListAsync(cancellationToken);
        fields.Add(draft);

        var values = await BoardItemFieldProjection.LoadAsync(context, fields, [item.Id], cancellationToken, currentUser.UserId);
        return values[item.Id].Computed[draft.Id.ToString()];
    }
}
