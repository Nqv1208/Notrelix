namespace Notrelix.Application.Common.Events;

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
/// Single authority for declared event evolution and recovery policy.
/// Contract registration consumes this policy; it must not maintain a second
/// event-name compatibility list.
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
        Policies.Single(policy =>
            string.Equals(policy.EventName, eventName, StringComparison.Ordinal)
            && policy.FromVersion == fromVersion
            && policy.ToVersion == toVersion);

    public static SchemaCompatibility GetCompatibility(string eventName, int version)
    {
        var hasDeclaredEvolution = Policies.Any(policy =>
            string.Equals(policy.EventName, eventName, StringComparison.Ordinal)
            && (policy.FromVersion == version || policy.ToVersion == version));

        return hasDeclaredEvolution
            ? SchemaCompatibility.None
            : SchemaCompatibility.Backward;
    }

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
