namespace Notrelix.Domain.Chat.Spaces;

/// <summary>
/// The kinds of space. Phase 1 creates channels only; the direct and group kinds exist so the persisted
/// vocabulary is complete from the start, and their commands arrive with direct and group conversations.
/// </summary>
public enum SpaceKind
{
    ChannelPublic,
    ChannelPrivate,
    Direct,
    Group,
}

/// <summary>The persisted, snake_case spelling of each enum value. The spelling is part of the data contract.</summary>
public static class SpaceWire
{
    public const string ChannelPublic = "channel_public";
    public const string ChannelPrivate = "channel_private";
    public const string Direct = "direct";
    public const string Group = "group";

    public static string ToWire(this SpaceKind kind) => kind switch
    {
        SpaceKind.ChannelPublic => ChannelPublic,
        SpaceKind.ChannelPrivate => ChannelPrivate,
        SpaceKind.Direct => Direct,
        SpaceKind.Group => Group,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown space kind."),
    };

    public static SpaceKind SpaceKindFromWire(string value) => value switch
    {
        ChannelPublic => SpaceKind.ChannelPublic,
        ChannelPrivate => SpaceKind.ChannelPrivate,
        Direct => SpaceKind.Direct,
        Group => SpaceKind.Group,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown space kind."),
    };

    public static string ToWire(this MembershipRole role) => role switch
    {
        MembershipRole.Manager => "manager",
        MembershipRole.Member => "member",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown membership role."),
    };

    public static MembershipRole MembershipRoleFromWire(string value) => value switch
    {
        "manager" => MembershipRole.Manager,
        "member" => MembershipRole.Member,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown membership role."),
    };

    public static string ToWire(this NotificationLevel level) => level switch
    {
        NotificationLevel.All => "all",
        NotificationLevel.Mentions => "mentions",
        NotificationLevel.None => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown notification level."),
    };

    public static NotificationLevel NotificationLevelFromWire(string value) => value switch
    {
        "all" => NotificationLevel.All,
        "mentions" => NotificationLevel.Mentions,
        "none" => NotificationLevel.None,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown notification level."),
    };
}
