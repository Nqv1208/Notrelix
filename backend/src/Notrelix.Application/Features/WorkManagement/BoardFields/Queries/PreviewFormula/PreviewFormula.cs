using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.BoardFields.Queries.PreviewFormula;

/// <summary>One value a draft read, named: a field a formula refers to, or the source field of one sub-item.</summary>
public record FormulaPreviewInputDto(string Name, ComputedValueDto Value);

/// <summary>
/// The draft's value (the same shape as a board read) and what it was computed from: for a formula the fields it
/// refers to on the item, for a rollup the source field's value of up to five sub-items and the sub-item count.
/// </summary>
public record FormulaPreviewDto(string State, JsonElement? Value, string? ErrorCode, IReadOnlyList<FormulaPreviewInputDto> Inputs, int? SubItemCount);

/// <summary>
/// What a draft formula or rollup would show on one Item of the Board, before it is saved: the same engine and the
/// same values as a board read, with the draft evaluated as one more calculated field that is never stored.
/// A formula is sent as <see cref="Expression"/>; a rollup as <see cref="RollupFunction"/> and, unless it counts,
/// <see cref="RollupFieldId"/>.
/// </summary>
public record PreviewFormulaQuery(Guid BoardId, Guid ItemId, string? Expression, string? RollupFunction = null, Guid? RollupFieldId = null) : IQuery<FormulaPreviewDto>, IRequirePermission, IAuthenticatedRequest, IReadRequest, IResourceScopedRequest
{
    public const int MaxExpressionLength = 2000;
    public const int MaxRollupInputs = 5;

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), ItemId);
}

public class PreviewFormulaQueryValidator : AbstractValidator<PreviewFormulaQuery>
{
    public PreviewFormulaQueryValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Expression).MaximumLength(PreviewFormulaQuery.MaxExpressionLength);
        RuleFor(x => x).Must(x => string.IsNullOrWhiteSpace(x.Expression) != string.IsNullOrWhiteSpace(x.RollupFunction))
            .WithMessage("Send either an expression or a rollup function.");
    }
}

public class PreviewFormulaQueryHandler(IWorkManagementDbContext context, ICurrentUser currentUser, IDateTimeProvider time)
    : IRequestHandler<PreviewFormulaQuery, FormulaPreviewDto>
{
    public async Task<FormulaPreviewDto> Handle(PreviewFormulaQuery request, CancellationToken cancellationToken)
    {
        var item = await context.BoardItems.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.BoardId == request.BoardId && i.DeletedAt == null, cancellationToken)
            ?? throw new NotFoundException(nameof(BoardItem), request.ItemId);

        var isRollup = !string.IsNullOrWhiteSpace(request.RollupFunction);
        BoardField draft;
        try
        {
            draft = Draft(item, isRollup
                ? JsonSerializer.Serialize(new { function = request.RollupFunction, fieldId = request.RollupFieldId })
                : JsonSerializer.Serialize(new { expression = request.Expression }), isRollup ? FieldType.Rollup : FieldType.Formula);
        }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException)
        {
            // A draft that does not parse has no value to show; the save would refuse it by the same rule.
            return new FormulaPreviewDto("error", null, BoardItemFieldProjection.ErrInvalidDefinition, [], null);
        }

        var fields = await context.BoardFields.AsNoTracking()
            .Include(f => f.Options)
            .Where(f => f.BoardId == request.BoardId && f.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var board = fields.ToList();

        var withDraft = fields.Append(draft).ToList();
        var values = await BoardItemFieldProjection.LoadAsync(context, withDraft, [item.Id], cancellationToken, currentUser.UserId);
        var computed = values[item.Id].Computed[draft.Id.ToString()];

        var inputs = new List<FormulaPreviewInputDto>();
        int? subItemCount = null;
        if (isRollup)
        {
            var children = await context.BoardItems.AsNoTracking()
                .Where(c => c.ParentItemId == item.Id && !c.IsArchived && c.DeletedAt == null)
                .OrderBy(c => c.Position)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            subItemCount = children.Count;
            var source = request.RollupFieldId is { } sourceId ? board.FirstOrDefault(f => f.Id == sourceId) : null;
            if (source is not null && children.Count > 0)
            {
                var shown = children.Take(PreviewFormulaQuery.MaxRollupInputs).ToList();
                var read = Draft(item, JsonSerializer.Serialize(new { expression = $"[{source.Id}]" }), FieldType.Formula);
                var childValues = await BoardItemFieldProjection.LoadAsync(context, board.Append(read).ToList(), shown, cancellationToken, currentUser.UserId);
                inputs.AddRange(shown.Select(id => new FormulaPreviewInputDto(source.Name, childValues[id].Computed[read.Id.ToString()])));
            }
        }
        else
        {
            foreach (var reference in ReferencesOf(request.Expression!))
            {
                var target = Guid.TryParse(reference, out var id)
                    ? board.FirstOrDefault(f => f.Id == id)
                    : board.FirstOrDefault(f => string.Equals(f.Name, reference, StringComparison.OrdinalIgnoreCase));
                if (target is null || inputs.Any(i => i.Name == target.Name)) continue;
                var read = Draft(item, JsonSerializer.Serialize(new { expression = $"[{target.Id}]" }), FieldType.Formula);
                var one = await BoardItemFieldProjection.LoadAsync(context, board.Append(read).ToList(), [item.Id], cancellationToken, currentUser.UserId);
                inputs.Add(new FormulaPreviewInputDto(target.Name, one[item.Id].Computed[read.Id.ToString()]));
            }
        }

        return new FormulaPreviewDto(computed.State, computed.Value, computed.ErrorCode, inputs, subItemCount);
    }

    private static IEnumerable<string> ReferencesOf(string expression)
    {
        try { return FormulaLanguage.References(FormulaLanguage.Parse(expression)).Distinct().ToList(); }
        catch (Notrelix.Domain.Common.Exceptions.BusinessRuleException) { return []; }
    }

    private BoardField Draft(BoardItem item, string settingsJson, FieldType type) =>
        BoardField.Create(
            item.AccountId, item.WorkspaceId, item.BoardId, "preview", type,
            FieldSettings.Create(JsonValue.Create(settingsJson)),
            FractionalIndex.Create("a0"), currentUser.UserId, time.UtcNow);
}
