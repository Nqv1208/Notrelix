namespace Notrelix.Domain.Analytics.Snapshots;

public class ReportingSnapshot : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string ReportType { get; private set; } = null!;
    public JsonValue Data { get; private set; } = null!;
    public DateTimeOffset CapturedAt { get; private set; }

    private ReportingSnapshot() : base() { }

    public static ReportingSnapshot Capture(Guid accountId, Guid workspaceId, string reportType, JsonValue data, DateTimeOffset capturedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(reportType);
        Guard.NotNull(data);
        Guard.NotEmpty(accountId);

        return new ReportingSnapshot
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ReportType = reportType,
            Data = data,
            CapturedAt = capturedAt
        };
    }
}
