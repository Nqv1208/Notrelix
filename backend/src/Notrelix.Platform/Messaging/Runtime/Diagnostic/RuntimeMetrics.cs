using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Notrelix.Platform.Messaging.Runtime;

public static class RuntimeMetrics
{
    private static readonly Meter Meter = new("Notrelix.Platform.Messaging.Runtime", "1.0.0");

    private static readonly Counter<long> PublishTotal =
        Meter.CreateCounter<long>("messaging.publish_total");

    private static readonly Counter<long> PublishFailed =
        Meter.CreateCounter<long>("messaging.publish_failed");

    private static readonly Counter<long> SchemaValidationFailed =
        Meter.CreateCounter<long>("messaging.schema_validation_failed");

    private static readonly Counter<long> GovernanceBlocked =
        Meter.CreateCounter<long>("messaging.governance_blocked");

    private static readonly Counter<long> GovernanceWarned =
        Meter.CreateCounter<long>("messaging.governance_warned");

    private static readonly ConcurrentDictionary<RuntimeStage, Histogram<double>> StageHistograms = new();

    private static Histogram<double> GetStageHistogram(RuntimeStage stage)
    {
        return StageHistograms.GetOrAdd(stage, s =>
            Meter.CreateHistogram<double>($"messaging.stage.{s.ToString().ToLowerInvariant()}_duration",
                unit: "ms"));
    }

    public static void RecordStage(RuntimeStage stage)
    {
        PublishTotal.Add(1);
        GetStageHistogram(stage).Record(0);
    }

    public static void IncrementSchemaValidationFailed()
    {
        SchemaValidationFailed.Add(1);
        PublishFailed.Add(1);
    }

    public static void IncrementGovernanceBlocked()
    {
        GovernanceBlocked.Add(1);
        PublishFailed.Add(1);
    }

    public static void IncrementGovernanceWarned()
    {
        GovernanceWarned.Add(1);
    }

    public static void IncrementPublished()
    {
    }
}
