using System.Diagnostics.Metrics;

namespace Notrelix.Infrastructure.Data.Rls;

/// <summary>
/// Counts entries into bounded database scopes. Labels are code-bounded (scope kind, reason category,
/// outcome); derived identifiers go to the structured log, never to a metric label.
/// </summary>
internal static class RlsScopeMetrics
{
    public const string MeterName = "Notrelix.Infrastructure.Rls";

    private static readonly Meter Meter = new(MeterName);

    private static readonly Counter<long> Entries = Meter.CreateCounter<long>(
        "rls_scope_entry",
        unit: "{entry}",
        description: "Entries into a bounded row-level-security scope.");

    public static void Record(string scopeKind, string reasonCategory, bool bound) =>
        Entries.Add(
            1,
            new KeyValuePair<string, object?>("scope", scopeKind),
            new KeyValuePair<string, object?>("reason", reasonCategory),
            new KeyValuePair<string, object?>("outcome", bound ? "bound" : "not_found"));
}
