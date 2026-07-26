using System.Text.Json;

namespace Notrelix.Domain.Analytics.Snapshots;

public sealed class ReportSnapshotPayload : ValueObject
{
    public string ReportType { get; }
    public int SchemaVersion { get; }
    public JsonValue Data { get; }

    private ReportSnapshotPayload(string reportType, int schemaVersion, JsonValue data)
    {
        ReportType = reportType;
        SchemaVersion = schemaVersion;
        Data = data;
    }

    public static ReportSnapshotPayload Create(string reportType, JsonValue data)
    {
        Guard.NotNullOrWhiteSpace(reportType);
        Guard.NotNull(data);

        try
        {
            using var document = JsonDocument.Parse(data.Value);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new BusinessRuleException(AnalyticsRuleCodes.Analytics_Snapshot_DataMustBeJsonObject, "Snapshot data must be a JSON object.");
        }
        catch (JsonException ex)
        {
            throw new BusinessRuleException(AnalyticsRuleCodes.Analytics_Snapshot_InvalidDataJson, $"Invalid snapshot data JSON: {ex.Message}");
        }

        return new ReportSnapshotPayload(reportType.Trim(), 1, data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ReportType;
        yield return SchemaVersion;
        yield return Data;
    }
}
