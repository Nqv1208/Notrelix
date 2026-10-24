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
}
