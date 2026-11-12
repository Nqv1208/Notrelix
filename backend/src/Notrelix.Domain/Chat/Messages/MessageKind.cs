namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// A message written by a person (<see cref="User"/>) or announced by the product for a change in a channel
/// (<see cref="System"/>, UC-CHT-B09). A system message is attributed to the person who caused the change.
/// </summary>
public enum MessageKind
{
    User,
    System,
}

/// <summary>The persisted, snake_case spelling of <see cref="MessageKind"/>. The spelling is part of the data contract.</summary>
public static class MessageKindWire
{
    public const string UserKind = "user";
    public const string SystemKind = "system";

    public static string ToWire(this MessageKind kind) => kind switch
    {
        MessageKind.User => UserKind,
        MessageKind.System => SystemKind,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown message kind."),
    };

    public static MessageKind MessageKindFromWire(string value) => value switch
    {
        UserKind => MessageKind.User,
        SystemKind => MessageKind.System,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown message kind."),
    };
}
