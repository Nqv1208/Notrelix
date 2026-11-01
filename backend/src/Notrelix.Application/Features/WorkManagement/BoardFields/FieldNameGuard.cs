using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields;

/// <summary>
/// A field name is unique on its board, compared trimmed and without regard to case: formulas read fields by
/// name, so two with one name would be ambiguous. Existing duplicates are left as they are; a rename of one
/// of them passes when the new name is free.
/// </summary>
public static class FieldNameGuard
{
    public const string NameTakenCode = "work-management.field.name-taken";
    public const string SystemLockedCode = "work-management.field.system-locked";

    public static async Task EnsureNameFreeAsync(
        IWorkManagementDbContext context, Guid boardId, Guid? exceptFieldId, string name, CancellationToken cancellationToken)
    {
        var wanted = name.Trim().ToLowerInvariant();
        var taken = await context.BoardFields.AsNoTracking()
            .AnyAsync(f => f.BoardId == boardId && f.DeletedAt == null && f.Id != exceptFieldId && f.Name.Trim().ToLower() == wanted, cancellationToken);
        if (taken)
            throw new ConflictException("A field with that name already exists on this board.", NameTakenCode);
    }
}
