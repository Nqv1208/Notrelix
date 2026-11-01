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
