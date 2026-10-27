using Microsoft.EntityFrameworkCore.Metadata;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Backfill-migration tests stop the schema at an old migration and seed rows with the current
/// model. A Workspace is therefore written with the columns that exist at that old schema only, so
/// later additive Workspace settings columns do not break these frozen-schema tests.
/// </summary>
internal static class LegacySchemaWorkspaceSeed
{
    public static Task<int> InsertAsync(ApplicationDbContext context, Workspace workspace) =>
        context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO workspace.workspaces
                (id, account_id, name, slug, status, is_personal, version, created_at, created_by)
            VALUES
                ({workspace.Id}, {workspace.AccountId}, {workspace.Name}, {workspace.Slug}, {workspace.Status.ToString()},
                 {workspace.IsPersonal}, {workspace.Version}, {workspace.CreatedAt}, {workspace.CreatedBy})
            """);

    /// <summary>
    /// The Item columns the current model writes that the held-back schema does not have yet, read from
    /// the model so a later migration that changes board_items never breaks the seed. They are added
    /// nullable; the returned names go to <see cref="DropLaterItemColumnsAsync"/>.
    /// </summary>
    // Identifiers come from the EF model, never from input, so the interpolated DDL is safe.
#pragma warning disable EF1002
    public static async Task<IReadOnlyList<string>> AddLaterItemColumnsAsync(ApplicationDbContext context)
    {
        var entity = context.Model.FindEntityType(typeof(BoardItem))!;
        var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        var existing = await context.Database
            .SqlQuery<string>($"SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_schema = {table.Schema} AND table_name = {table.Name}")
            .ToListAsync();
        var missing = entity.GetProperties()
            .Select(p => (Name: p.GetColumnName(table), Type: p.GetColumnType(table)))
            .Where(c => c.Name is not null && !existing.Contains(c.Name))
            .ToList();
        foreach (var column in missing)
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table.Schema}\".\"{table.Name}\" ADD COLUMN \"{column.Name}\" {column.Type} NULL");
        return missing.Select(c => c.Name!).ToList();
    }

    /// <summary>Takes the columns added for the seed off again so the migrations that own them can add them.</summary>
    public static async Task DropLaterItemColumnsAsync(ApplicationDbContext context, IReadOnlyList<string> added)
    {
        foreach (var column in added)
            await context.Database.ExecuteSqlRawAsync($"ALTER TABLE work.board_items DROP COLUMN IF EXISTS \"{column}\"");
    }
#pragma warning restore EF1002
}
