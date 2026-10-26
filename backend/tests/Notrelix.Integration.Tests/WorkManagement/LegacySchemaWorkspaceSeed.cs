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
    /// The Item columns that later migrations add. The current model writes them when a test seeds
    /// Items through the domain, so a database held at an older schema gets them for the seed.
    /// </summary>
    public static Task<int> AddLaterItemColumnsAsync(ApplicationDbContext context) =>
        context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE work.board_items
                ADD COLUMN IF NOT EXISTS archived_at timestamptz NULL,
                ADD COLUMN IF NOT EXISTS archived_by uuid NULL,
                ADD COLUMN IF NOT EXISTS completed_by uuid NULL,
                ADD COLUMN IF NOT EXISTS status_before_completion_option_id uuid NULL,
                ADD COLUMN IF NOT EXISTS archived_with_parent boolean NOT NULL DEFAULT false
            """);

    /// <summary>Takes them off again so the migrations that own them can add them.</summary>
    public static Task<int> DropLaterItemColumnsAsync(ApplicationDbContext context) =>
        context.Database.ExecuteSqlRawAsync("""
            ALTER TABLE work.board_items
                DROP COLUMN IF EXISTS archived_at,
                DROP COLUMN IF EXISTS archived_by,
                DROP COLUMN IF EXISTS completed_by,
                DROP COLUMN IF EXISTS status_before_completion_option_id,
                DROP COLUMN IF EXISTS archived_with_parent
            """);
}
