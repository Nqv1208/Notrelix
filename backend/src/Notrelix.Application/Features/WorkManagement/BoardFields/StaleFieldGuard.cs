using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardFields;

/// <summary>
/// A write against a field the board no longer shows. When the field was deleted (the client holds an old schema)
/// the answer is a precondition failure naming who deleted it, so the client can say so and reload the schema;
/// nothing is stored. A field the board never had stays an identical NotFound.
/// </summary>
internal static class StaleFieldGuard
{
    public const string DeletedCode = "work-management.field.deleted";
    public const string OptionRemovedCode = "work-management.field.option-removed";

    /// <summary>
    /// A select value names an option the field no longer has (the client holds an old schema): a precondition failure
    /// naming the field and the option, nothing stored. An option is hard-removed, so this cannot tell it from an id
    /// the field never had; both are a stale or wrong client and get the same answer.
    /// </summary>
    public static void EnsureOptionsExist(BoardField field, FieldValue value)
    {
        if (value.IsEmpty || field.Type is not (FieldType.Select or FieldType.Status or FieldType.MultiSelect)) return;
        using var doc = System.Text.Json.JsonDocument.Parse(value.Data.Value);
        var root = doc.RootElement;
        var named = root.ValueKind == System.Text.Json.JsonValueKind.Array ? root.EnumerateArray().ToList() : [root];
        var known = field.Options.Select(option => option.Id).ToHashSet();
        foreach (var element in named)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.String || !Guid.TryParse(element.GetString(), out var id) || known.Contains(id)) continue;
            throw new Notrelix.Application.Common.Exceptions.PreconditionFailedException(
                "An option of the field was removed since this client loaded the board.", OptionRemovedCode)
            {
                Extensions = new Dictionary<string, object?> { ["fieldName"] = field.Name, ["optionId"] = id },
            };
        }
    }

    public static async Task<Exception> MissingAsync(
        IWorkManagementDbContext context,
        IActorLookupService? actors,
        ICurrentRequestContext requestContext,
        Guid boardId,
        Guid fieldId,
        CancellationToken ct)
    {
        var accountId = requestContext.RequireAccountId();
        var workspaceId = requestContext.RequireWorkspaceId();
        var deleted = await context.BoardFields
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f => f.Id == fieldId && f.BoardId == boardId && f.AccountId == accountId && f.WorkspaceId == workspaceId && f.DeletedAt != null)
            .Select(f => new { f.Name, f.DeletedBy })
            .FirstOrDefaultAsync(ct);
        if (deleted is null) return new NotFoundException("BoardField", fieldId);

        string? by = null;
        if (actors is not null && deleted.DeletedBy is { } actorId)
            by = (await actors.FindManyAsync([actorId], ct)).FirstOrDefault()?.Name;
        return new Notrelix.Application.Common.Exceptions.PreconditionFailedException(
            "The field was deleted since this client loaded the board.", DeletedCode)
        {
            Extensions = new Dictionary<string, object?> { ["fieldName"] = deleted.Name, ["changedByName"] = by },
        };
    }
}
