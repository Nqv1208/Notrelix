using System.Data;

namespace Notrelix.Infrastructure.Data.Services;

public sealed class ResourceVersionReader : IResourceVersionReader
{
    private static readonly Dictionary<ResourceKind, (string Schema, string Table)> TableMap = new()
    {
        [ResourceKind.Create("work-management.board")] = (DbSchemas.Work, "boards"),
        [ResourceKind.Create("work-management.board-item")] = (DbSchemas.Work, "board_items"),
        [ResourceKind.Create("work-management.board-group")] = (DbSchemas.Work, "board_groups"),
        [ResourceKind.Create("work-management.board-field")] = (DbSchemas.Work, "board_fields"),
        [ResourceKind.Create("work-management.board-view")] = (DbSchemas.Work, "board_views"),
        [ResourceKind.Create("workspaces.workspace")] = (DbSchemas.Workspace, "workspaces"),
        [ResourceKind.Create("documents.page")] = (DbSchemas.Docs, "pages"),
        [ResourceKind.Create("documents.block")] = (DbSchemas.Docs, "blocks"),
        [ResourceKind.Create("collaboration.comment")] = (DbSchemas.Collab, "comments"),
        [ResourceKind.Create("automation.rule")] = (DbSchemas.Automation, "automation_rules"),
        [ResourceKind.Create("work-management.form")] = (DbSchemas.Work, "forms"),
        [ResourceKind.Create("work-management.checklist")] = (DbSchemas.Work, "checklists"),
        [ResourceKind.Create("work-management.approval-request")] = (DbSchemas.Work, "approval_requests"),
        [ResourceKind.Create("analytics.dashboard")] = (DbSchemas.Work, "dashboards"),
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<ResourceVersionReader> _logger;

    public ResourceVersionReader(
        ApplicationDbContext db,
        ILogger<ResourceVersionReader> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<long?> GetVersionAsync(ResourceRef resource, CancellationToken cancellationToken)
    {
        if (!TableMap.TryGetValue(resource.Kind, out var mapping))
        {
            throw new NotSupportedException(
                $"ResourceKind '{resource.Kind.Value}' is not supported for version reading. " +
                $"Add a table mapping in {nameof(ResourceVersionReader)}.");
        }

        var connection = _db.Database.GetDbConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT version FROM {mapping.Schema}.{mapping.Table} WHERE id = @id";

        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "id";
        idParam.Value = resource.ResourceId;
        idParam.DbType = DbType.Guid;
        cmd.Parameters.Add(idParam);

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);

        if (result is long longVersion)
            return longVersion;

        return null;
    }
}
