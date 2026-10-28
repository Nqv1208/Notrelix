using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement.Formulas;

namespace Notrelix.Application.Features.WorkManagement.BoardFields;

/// <summary>
/// Saving a Formula or Rollup checks it against the Board's other fields: an unknown reference or a
/// cycle is rejected with its rule code and nothing is saved. Other field types are not checked.
/// </summary>
public static class ComputedFieldGuard
{
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
