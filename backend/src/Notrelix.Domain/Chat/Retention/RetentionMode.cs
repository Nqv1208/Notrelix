namespace Notrelix.Domain.Chat.Retention;

/// <summary>What the retention job does to a message that is older than the Workspace's retention window (UC-CHT-H05).</summary>
public enum RetentionMode
{
    /// <summary>The message becomes a tombstone: the author, time and sequence stay, the content and what hangs on it go.</summary>
    Tombstone,

    /// <summary>The message row is deleted, with its reactions, mentions, pin and files. A thread goes as a whole, once all of it is old.</summary>
    Purge,
}

public static class RetentionModeWire
{
    public const string TombstoneWire = "tombstone";
    public const string PurgeWire = "purge";

    public static string ToWire(this RetentionMode mode) => mode switch
    {
        RetentionMode.Tombstone => TombstoneWire,
        RetentionMode.Purge => PurgeWire,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown retention mode."),
    };

    /// <summary>The mode of a wire name, or null when the name is not a mode.</summary>
    public static RetentionMode? FromWire(string? value) => value switch
    {
        TombstoneWire => RetentionMode.Tombstone,
        PurgeWire => RetentionMode.Purge,
        _ => null,
    };
}
