using static Notrelix.Domain.Analytics.AnalyticsRuleCodes;

namespace Notrelix.Domain.Analytics.Snapshots;

public class ReportingSnapshot : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ReportType { get; private set; } = null!;
    public int SchemaVersion { get; private set; }
    public JsonValue Data { get; private set; } = null!;
    public ReportSnapshotPayload Payload => ReportSnapshotPayload.Create(ReportType, SchemaVersion, Data);
    public DateTimeOffset CapturedAt { get; private set; }

    private ReportingSnapshot() : base() { }

    public static ReportingSnapshot Capture(Guid accountId, Guid workspaceId, ReportSnapshotPayload payload, DateTimeOffset capturedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(payload);
        if (capturedAt == default)
            throw new BusinessRuleException(Analytics_Snapshot_CapturedAtDefault, "CapturedAt must not be the default value.");

        return new ReportingSnapshot
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ReportType = payload.ReportType,
            SchemaVersion = payload.SchemaVersion,
            Data = payload.Data,
            CapturedAt = capturedAt
        };
    }
}
