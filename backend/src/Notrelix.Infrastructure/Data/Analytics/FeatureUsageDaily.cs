using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Analytics;

public sealed class FeatureUsageDaily
{
    public Guid WorkspaceId { get; private set; }
    public DateOnly UsageDate { get; private set; }
    public string FeatureCode { get; private set; } = null!;
    public long UsageCount { get; private set; }
    public int UniqueActorCount { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Unit { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset CalculatedAt { get; private set; }
    public DateTimeOffset? SourceWatermarkAt { get; private set; }

    private FeatureUsageDaily() { }

    public FeatureUsageDaily(
        Guid workspaceId,
        DateOnly usageDate,
        string featureCode,
        long usageCount,
        int uniqueActorCount,
        decimal quantity,
        string? unit,
        JsonDocument? metadataJson,
        DateTimeOffset calculatedAt)
    {
        WorkspaceId = workspaceId;
        UsageDate = usageDate;
        FeatureCode = featureCode;
        UsageCount = usageCount;
        UniqueActorCount = uniqueActorCount;
        Quantity = quantity;
        Unit = unit;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        CalculatedAt = calculatedAt;
    }
}
