using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Audit;

public class AuditRetentionPolicy : Entity
{
    public Guid WorkspaceId { get; private set; }
    public int RetentionDays { get; private set; }
    public bool ExportBeforeDelete { get; private set; }
    public JsonValue PolicyJson { get; private set; } = null!;

    private AuditRetentionPolicy() : base() { }

    public static AuditRetentionPolicy Create(Guid workspaceId, int retentionDays = 365, bool exportBeforeDelete = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.Positive(retentionDays);

        return new AuditRetentionPolicy
        {
            WorkspaceId = workspaceId,
            RetentionDays = retentionDays,
            ExportBeforeDelete = exportBeforeDelete,
            PolicyJson = JsonValue.EmptyObject()
        };
    }
}
