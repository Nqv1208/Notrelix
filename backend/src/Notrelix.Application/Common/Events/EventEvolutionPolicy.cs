namespace Notrelix.Application.Common.Events;

public enum UpcastMode
{
    Forbidden,
    Allowed,
}

public enum CutoverMode
{
    DrainBeforeCutover,
}

public enum RecoveryMode
{
    Replay,
    RebuildFromAuthority,
    NotReplayable,
}

public sealed record EventEvolutionPolicy(
    string EventName,
    int FromVersion,
    int ToVersion,
    UpcastMode UpcastMode,
    CutoverMode CutoverMode,
    RecoveryMode RecoveryMode)
{
    public SchemaCompatibility Compatibility =>
        UpcastMode == UpcastMode.Forbidden
            ? SchemaCompatibility.None
            : SchemaCompatibility.Backward;

    public bool SyntheticUpcastAllowed => UpcastMode == UpcastMode.Allowed;
}

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
        var policy = Policies.FirstOrDefault(policy =>
            string.Equals(policy.EventName, eventName, StringComparison.Ordinal)
            && (policy.FromVersion == version || policy.ToVersion == version));

        return policy?.Compatibility ?? SchemaCompatibility.Backward;
    }

    private static EventEvolutionPolicy Create(string eventName) =>
        new(
            EventName: eventName,
            FromVersion: 1,
            ToVersion: 2,
            UpcastMode: UpcastMode.Forbidden,
            CutoverMode: CutoverMode.DrainBeforeCutover,
            RecoveryMode: RecoveryMode.RebuildFromAuthority);
}
