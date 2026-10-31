using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.BoardFields;

/// <summary>
/// Saving a Formula or Rollup checks it against the Board's other fields: an unknown reference or a
/// cycle is rejected with its rule code and nothing is saved. Other field types are not checked.
/// </summary>
public static class ComputedFieldGuard
{
    /// <summary>
    /// A Formula's name references become field ids before it is saved, so renaming a field keeps the
    /// formula working. <paramref name="self"/> is the field being updated (null on create).
    /// </summary>
    public static async Task<string> NormalizeAsync(
        IWorkManagementDbContext context, Guid boardId, ComputedFieldInput? self, FieldType type, string settings, CancellationToken cancellationToken)
    {
        if (type != FieldType.Formula) return settings;
        var others = await context.BoardFields.AsNoTracking()
            .Where(f => f.BoardId == boardId && f.DeletedAt == null && (self == null || f.Id != self.Id))
            .ToListAsync(cancellationToken);
        var board = others.Select(f => new ComputedFieldInput(f.Id, f.Name, f.Type, f.Settings.Data.Value)).ToList();
        if (self is not null) board.Add(self);
        return ComputedFieldGraph.NormalizeToIds(settings, board);
    }

    /// <summary>
    /// A field that a Formula or Rollup reads cannot be deleted: the computed field would lose its input and
    /// every item would show an error. The refusal names the first field that depends on it.
    /// </summary>
    public static async Task EnsureNoDependentsAsync(IWorkManagementDbContext context, BoardField target, CancellationToken cancellationToken)
    {
        var fields = await context.BoardFields.AsNoTracking()
            .Where(f => f.BoardId == target.BoardId && f.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var board = fields.Select(f => new ComputedFieldInput(f.Id, f.Name, f.Type, f.Settings.Data.Value)).ToList();
        foreach (var computed in board.Where(f => f.Id != target.Id && f.Type is FieldType.Formula or FieldType.Rollup))
        {
            IReadOnlyList<Guid> reads;
            try { reads = ComputedFieldGraph.DependenciesOf(computed, board); }
            catch (BusinessRuleException) { continue; }
            if (reads.Contains(target.Id))
                throw new BusinessRuleException($"'{computed.Name}' reads this field. Change or delete it first.");
        }
    }

    public static async Task EnsureAsync(IWorkManagementDbContext context, BoardField candidate, CancellationToken cancellationToken)
    {
        if (candidate.Type is not (FieldType.Formula or FieldType.Rollup)) return;

        var others = await context.BoardFields.AsNoTracking()
            .Where(f => f.BoardId == candidate.BoardId && f.Id != candidate.Id && f.DeletedAt == null)
            .ToListAsync(cancellationToken);
        var board = others
            .Select(f => new ComputedFieldInput(f.Id, f.Name, f.Type, f.Settings.Data.Value))
            .Append(new ComputedFieldInput(candidate.Id, candidate.Name, candidate.Type, candidate.Settings.Data.Value))
            .ToList();
        ComputedFieldGraph.EnsureValid(board);
    }
}
