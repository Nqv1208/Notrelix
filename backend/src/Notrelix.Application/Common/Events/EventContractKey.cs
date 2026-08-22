namespace Notrelix.Application.Common.Events;

/// <summary>
/// Canonical public integration-event contract identity (IAREQ088 / IAREQ131).
/// Runtime resolution of public integration events MUST use the compound
/// (Name, Version) key. Name alone is never a sufficient contract identity;
/// no implicit latest/oldest/v1 fallback exists.
/// </summary>
public sealed record EventContractKey
{
    public string Name { get; }
    public int Version { get; }

    public EventContractKey(string name, int version)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event contract name cannot be empty.", nameof(name));
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), version, "Version must be a positive integer.");

        Name = name;
        Version = version;
    }

    public override string ToString() => $"{Name} v{Version}";
}
