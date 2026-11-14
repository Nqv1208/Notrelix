namespace Notrelix.Domain.Chat.Permissions;

/// <summary>
/// The actions a channel can override per role (UC-CHT-H01). Each one maps to a Governance chat action; attach rides on posting
/// because a file belongs to a message.
/// </summary>
public enum ChannelAction
{
    Post,
    React,
    Attach,
    Pin,
    Broadcast,
    Invite,
}

/// <summary>
/// The roles of a channel an override can address. A Workspace Owner or Admin is never a subject: they always keep every channel
/// action, so a channel cannot lock them out (UC-CHT-H01). A channel manager is matched before the Workspace role, so a read-only
/// channel can still let its managers post.
/// </summary>
public enum ChannelPermissionSubject
{
    Manager,
    Member,
    Guest,
}

public enum ChannelPermissionEffect
{
    Allow,
    Deny,
}

/// <summary>The persisted, snake_case spelling of each value. The spelling is part of the data and API contract.</summary>
public static class ChannelPermissionWire
{
    public static string ToWire(this ChannelAction action) => action switch
    {
        ChannelAction.Post => "post",
        ChannelAction.React => "react",
        ChannelAction.Attach => "attach",
        ChannelAction.Pin => "pin",
        ChannelAction.Broadcast => "broadcast",
        ChannelAction.Invite => "invite",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown channel action."),
    };

    public static ChannelAction? ActionFromWire(string? value) => value switch
    {
        "post" => ChannelAction.Post,
        "react" => ChannelAction.React,
        "attach" => ChannelAction.Attach,
        "pin" => ChannelAction.Pin,
        "broadcast" => ChannelAction.Broadcast,
        "invite" => ChannelAction.Invite,
        _ => null,
    };

    public static string ToWire(this ChannelPermissionSubject subject) => subject switch
    {
        ChannelPermissionSubject.Manager => "manager",
        ChannelPermissionSubject.Member => "member",
        ChannelPermissionSubject.Guest => "guest",
        _ => throw new ArgumentOutOfRangeException(nameof(subject), subject, "Unknown permission subject."),
    };

    public static ChannelPermissionSubject? SubjectFromWire(string? value) => value switch
    {
        "manager" => ChannelPermissionSubject.Manager,
        "member" => ChannelPermissionSubject.Member,
        "guest" => ChannelPermissionSubject.Guest,
        _ => null,
    };

    public static string ToWire(this ChannelPermissionEffect effect) => effect switch
    {
        ChannelPermissionEffect.Allow => "allow",
        ChannelPermissionEffect.Deny => "deny",
        _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown permission effect."),
    };

    public static ChannelPermissionEffect? EffectFromWire(string? value) => value switch
    {
        "allow" => ChannelPermissionEffect.Allow,
        "deny" => ChannelPermissionEffect.Deny,
        _ => null,
    };
}
