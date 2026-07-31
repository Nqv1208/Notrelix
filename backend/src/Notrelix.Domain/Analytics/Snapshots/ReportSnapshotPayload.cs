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

    public static ReportSnapshotPayload Create(string reportType, int schemaVersion, JsonValue data)
    {
        Guard.NotNullOrWhiteSpace(reportType);
        Guard.NotNull(data);
        if (schemaVersion <= 0)
            throw new BusinessRuleException(AnalyticsRuleCodes.Analytics_Snapshot_SchemaVersionMustBePositive, "Snapshot schema version must be greater than zero.");

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

        return new ReportSnapshotPayload(reportType.Trim(), schemaVersion, data);
    }

    /// <summary>
    /// Convenience factory for newly captured snapshots.
    /// Must not be used during rehydration — use <see cref="Create(string,int,JsonValue)"/>
    /// with the stored schema version instead.
    /// </summary>
    public static ReportSnapshotPayload CreateV1(string reportType, JsonValue data) =>
        Create(reportType, 1, data);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ReportType;
        yield return SchemaVersion;
        yield return Data;
    }
}
