using System.Data;

namespace Notrelix.Infrastructure.Data.Services;

public sealed class ResourceVersionReader : IResourceVersionReader
{
    private static readonly Dictionary<ResourceType, (string Schema, string Table)> TableMap = new()
    {
        [ResourceType.Board] = (DbSchemas.Work, "boards"),
        [ResourceType.BoardItem] = (DbSchemas.Work, "board_items"),
        [ResourceType.BoardGroup] = (DbSchemas.Work, "board_groups"),
        [ResourceType.BoardField] = (DbSchemas.Work, "board_fields"),
        [ResourceType.BoardView] = (DbSchemas.Work, "board_views"),
        [ResourceType.Workspace] = (DbSchemas.Workspace, "workspaces"),
        [ResourceType.Page] = (DbSchemas.Docs, "pages"),
        [ResourceType.Block] = (DbSchemas.Docs, "blocks"),
        [ResourceType.Comment] = (DbSchemas.Collab, "comments"),
        [ResourceType.AutomationRule] = (DbSchemas.Automation, "automation_rules"),
        [ResourceType.Form] = (DbSchemas.Work, "forms"),
        [ResourceType.Checklist] = (DbSchemas.Work, "checklists"),
        [ResourceType.ApprovalRequest] = (DbSchemas.Work, "approval_requests"),
        [ResourceType.Dashboard] = (DbSchemas.Work, "dashboards"),
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
        if (!LegacyResourceTypeMappings.TryToLegacyEnum(resource.Kind.Value, out var resourceType)
            || !TableMap.TryGetValue(resourceType, out var mapping))
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
