namespace Notrelix.Platform.Messaging.Contracts.Evolution;

public enum EventEvolutionDisposition
{
    DrainBeforeCutover,
    Upcastable,
    ReplayableSameSchema,
    RebuildFromAuthority,
    NotReplayable,
}

public sealed record EventEvolutionPolicy(
    string EventName,
    int FromVersion,
    int ToVersion,
    EventEvolutionDisposition Disposition,
    string Recovery,
    string CutoverRequirement,
    bool SyntheticUpcastAllowed);

/// <summary>
/// Explicit policy for event transitions whose new schema carries a producer
/// revision that cannot be reconstructed from a v1 payload. This registry is
/// intentionally separate from the generic compatibility evaluator: transport
/// compatibility must not imply replay compatibility.
/// </summary>
public static class EventEvolutionPolicyRegistry
{
    private static readonly IReadOnlyList<EventEvolutionPolicy> Policies =
    [
        Create("board.item.created"),
        Create("board_item.moved"),
        Create("board_item.archived"),
    ];

    public static IReadOnlyList<EventEvolutionPolicy> GetAll() => Policies;

    public static EventEvolutionPolicy Get(string eventName, int fromVersion, int toVersion) =>
        Policies.Single(p =>
            string.Equals(p.EventName, eventName, StringComparison.Ordinal)
            && p.FromVersion == fromVersion
            && p.ToVersion == toVersion);

    private static EventEvolutionPolicy Create(string eventName) =>
        new(
            EventName: eventName,
            FromVersion: 1,
            ToVersion: 2,
            Disposition: EventEvolutionDisposition.DrainBeforeCutover,
            Recovery: "V1 cannot mutate the V2 revision projection; rebuild from the authoritative Work producer snapshot.",
            CutoverRequirement: "Drain or quarantine every V1 queue before V2-only consumers become authoritative.",
            SyntheticUpcastAllowed: false);
}
